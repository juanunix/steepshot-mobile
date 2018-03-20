﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ditch.Core.Helpers;
using Ditch.Steem;
using Ditch.Steem.Operations;
using Steepshot.Core.Models.Common;
using Steepshot.Core.Models.Requests;
using Steepshot.Core.Models.Responses;
using Steepshot.Core.Serializing;
using DitchBeneficiary = Ditch.Steem.Operations.Beneficiary;
using Ditch.Core;
using Ditch.Steem.Models.Args;
using Steepshot.Core.Errors;
using Steepshot.Core.Models.Enums;
using Steepshot.Core.Localization;
using Ditch.Steem.Models.Objects;
using Ditch.Steem.Models;

namespace Steepshot.Core.HttpClient
{
    internal class SteemClient : BaseDitchClient
    {
        private readonly OperationManager _operationManager;

        public override bool IsConnected => _operationManager.IsConnected;

        public SteemClient(JsonNetConverter jsonConverter) : base(jsonConverter)
        {
            var jss = GetJsonSerializerSettings();
            var cm = new HttpManager(jss);
            _operationManager = new OperationManager(cm, jss);
        }

        public override bool TryReconnectChain(CancellationToken token)
        {
            if (EnableWrite)
                return EnableWrite;

            var lockWasTaken = false;
            try
            {
                Monitor.Enter(SyncConnection, ref lockWasTaken);
                if (!EnableWrite)
                {
                    var cUrls = new List<string> { "https://api.steemit.com", "https://steemd.steepshot.org" };
                    var conectedTo = _operationManager.TryConnectTo(cUrls, token);
                    if (!string.IsNullOrEmpty(conectedTo))
                        EnableWrite = true;
                }
            }
            catch (Exception)
            {
                //todo nothing
            }
            finally
            {
                if (lockWasTaken)
                    Monitor.Exit(SyncConnection);
            }
            return EnableWrite;
        }

        #region Post requests

        public override async Task<OperationResult<VoteResponse>> Vote(VoteModel model, CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                if (!TryReconnectChain(ct))
                    return new OperationResult<VoteResponse>(new AppError(LocalizationKeys.EnableConnectToBlockchain));

                var keys = ToKeyArr(model.PostingKey);
                if (keys == null)
                    return new OperationResult<VoteResponse>(new AppError(LocalizationKeys.WrongPrivatePostingKey));

                short weigth = 0;
                if (model.Type == VoteType.Up)
                    weigth = 10000;
                if (model.Type == VoteType.Flag)
                    weigth = -10000;

                var op = new VoteOperation(model.Login, model.Author, model.Permlink, weigth);
                var resp = _operationManager.BroadcastOperationsSynchronous(keys, ct, op);

                var result = new OperationResult<VoteResponse>();
                if (!resp.IsError)
                {
                    var dt = DateTime.Now;
                    var args = new FindCommentsArgs()
                    {
                        Comments = new[] { new[] { model.Author, model.Permlink } }
                    };
                    var content = _operationManager.FindComments(args, ct);
                    if (!content.IsError && content.Result.Comments.Any())
                    {
                        var comment = content.Result.Comments[0];
                        //Convert Asset type to double
                        result.Result = new VoteResponse(true)
                        {
                           // NewTotalPayoutReward = //comment.TotalPayoutValue + comment.CuratorPayoutValue + comment.PendingPayoutValue,
                            NetVotes = comment.NetVotes,
                            VoteTime = dt
                        };
                    }
                }
                else
                {
                    OnError(resp, result);
                }
                return result;
            }, ct);
        }

        public override async Task<OperationResult<VoidResponse>> Follow(FollowModel model, CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                if (!TryReconnectChain(ct))
                    return new OperationResult<VoidResponse>(new AppError(LocalizationKeys.EnableConnectToBlockchain));

                var keys = ToKeyArr(model.PostingKey);
                if (keys == null)
                    return new OperationResult<VoidResponse>(new AppError(LocalizationKeys.WrongPrivatePostingKey));

                var op = model.Type == FollowType.Follow
                    ? new FollowOperation(model.Login, model.Username, Ditch.Steem.Models.Enums.FollowType.Blog, model.Login)
                    : new UnfollowOperation(model.Login, model.Username, model.Login);
                var resp = _operationManager.BroadcastOperationsSynchronous(keys, ct, op);

                var result = new OperationResult<VoidResponse>();

                if (!resp.IsError)
                    result.Result = new VoidResponse(true);
                else
                    OnError(resp, result);

                return result;
            }, ct);
        }

        public override async Task<OperationResult<VoidResponse>> LoginWithPostingKey(AuthorizedModel model, CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                if (!TryReconnectChain(ct))
                    return new OperationResult<VoidResponse>(new AppError(LocalizationKeys.EnableConnectToBlockchain));

                var keys = ToKeyArr(model.PostingKey);
                if (keys == null)
                    return new OperationResult<VoidResponse>(new AppError(LocalizationKeys.WrongPrivatePostingKey));

                var op = new FollowOperation(model.Login, "steepshot", Ditch.Steem.Models.Enums.FollowType.Blog, model.Login);
                var resp = _operationManager.VerifyAuthority(keys, ct, op);

                var result = new OperationResult<VoidResponse>();

                if (!resp.IsError)
                    result.Result = new VoidResponse(true);
                else
                    OnError(resp, result);

                return result;
            }, ct);
        }

        public override async Task<OperationResult<VoidResponse>> CreateOrEdit(CommentModel model, CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                if (!TryReconnectChain(ct))
                    return new OperationResult<VoidResponse>(new AppError(LocalizationKeys.EnableConnectToBlockchain));

                var keys = ToKeyArr(model.PostingKey);
                if (keys == null)
                    return new OperationResult<VoidResponse>(new AppError(LocalizationKeys.WrongPrivatePostingKey));

                var op = new CommentOperation(model.ParentAuthor, model.ParentPermlink, model.Author, model.Permlink, model.Title, model.Body, model.JsonMetadata);

                BaseOperation[] ops;
                if (model.Beneficiaries != null && model.Beneficiaries.Any())
                {
                    var beneficiaries = model.Beneficiaries
                        .Select(i => new DitchBeneficiary(i.Account, i.Weight))
                        .ToArray();
                    ops = new BaseOperation[]
                    {
                        op,
                        new BeneficiariesOperation(model.Login, model.Permlink, new Asset(1000000000, Config.SteemAssetNumSbd), beneficiaries)
                    };
                }
                else
                {
                    ops = new BaseOperation[] { op };
                }

                var resp = _operationManager.BroadcastOperationsSynchronous(keys, ct, ops);

                var result = new OperationResult<VoidResponse>();
                if (!resp.IsError)
                {
                    result.Result = new VoidResponse(true);
                }
                else
                    OnError(resp, result);

                return result;
            }, ct);
        }

        public override async Task<OperationResult<VoidResponse>> Delete(DeleteModel model, CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                if (!TryReconnectChain(ct))
                    return new OperationResult<VoidResponse>(new AppError(LocalizationKeys.EnableConnectToBlockchain));

                var keys = ToKeyArr(model.PostingKey);
                if (keys == null)
                    return new OperationResult<VoidResponse>(new AppError(LocalizationKeys.WrongPrivatePostingKey));

                var op = new DeleteCommentOperation(model.Author, model.Permlink);
                var resp = _operationManager.BroadcastOperationsSynchronous(keys, ct, op);

                var result = new OperationResult<VoidResponse>();
                if (!resp.IsError)
                    result.Result = new VoidResponse(true);
                else
                    OnError(resp, result);
                return result;
            }, ct);
        }

        public override async Task<OperationResult<VoidResponse>> UpdateUserProfile(UpdateUserProfileModel model, CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                if (!TryReconnectChain(ct))
                    return new OperationResult<VoidResponse>(new AppError(LocalizationKeys.EnableConnectToBlockchain));

                var keys = ToKeyArr(model.ActiveKey);
                if (keys == null)
                    return new OperationResult<VoidResponse>(new AppError(LocalizationKeys.WrongPrivateActimeKey));

                var args = new FindAccountsArgs { Accounts = new[] { model.Login } };

                var resp = _operationManager.FindAccounts(args, CancellationToken.None);
                var result = new OperationResult<VoidResponse>();
                if (resp.IsError)
                {
                    OnError(resp, result);
                    return result;
                }

                var profile = resp.Result?.Accounts.Length == 1 ? resp.Result?.Accounts[0] : null;
                if (profile == null)
                {
                    result.Error = new BlockchainError(LocalizationKeys.UnexpectedProfileData);
                    return result;
                }

                var editedMeta = UpdateProfileJson(profile.JsonMetadata, model);

                var op = new AccountUpdateOperation(model.Login, profile.MemoKey, editedMeta);
                var resp2 = _operationManager.BroadcastOperationsSynchronous(keys, ct, op);
                if (!resp2.IsError)
                    result.Result = new VoidResponse(true);
                else
                    OnError(resp2, result);
                return result;
            }, ct);
        }


        #endregion Post requests

        #region Get

        public override async Task<OperationResult<string>> GetVerifyTransaction(UploadMediaModel model, CancellationToken ct)
        {
            if (!TryReconnectChain(ct))
                return new OperationResult<string>(new AppError(LocalizationKeys.EnableConnectToBlockchain));

            var keys = ToKeyArr(model.PostingKey);
            if (keys == null)
                return new OperationResult<string>(new AppError(LocalizationKeys.WrongPrivatePostingKey));

            return await Task.Run(() =>
            {
                var op = new FollowOperation(model.Login, "steepshot", Ditch.Steem.Models.Enums.FollowType.Blog, model.Login);
                var properties = new DynamicGlobalPropertyApiObj
                {
                    HeadBlockId = Hex.ToString(_operationManager.ChainId),
                    Time = DateTime.Now,
                    HeadBlockNumber = 0
                };
                var tr = _operationManager.CreateTransaction(properties, keys, ct, op);
                return new OperationResult<string>() { Result = JsonConverter.Serialize(tr) };
            }, ct);
        }

        #endregion
    }
}
