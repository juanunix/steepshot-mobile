﻿using Newtonsoft.Json;
using Steepshot.Core.Authority;
using System.ComponentModel.DataAnnotations;
using Ditch.Core.Helpers;
using Steepshot.Core.Localization;
using Steepshot.Core.Models.Common;
using System.Diagnostics;

namespace Steepshot.Core.Models.Requests
{
    [DebuggerStepThrough]
    [JsonObject(MemberSerialization.OptIn)]
    public class PreparePostModel : AuthorizedModel
    {
        private string[] _tags;
        private string _permlink;
        public const int TagLimit = 20;

        [JsonProperty]
        public string Description { get; set; }
        
        public string Permlink
        {
            get
            {
                if (string.IsNullOrEmpty(_permlink))
                    _permlink = OperationHelper.TitleToPermlink(Title);

                return _permlink;
            }
        }

        //needed for post/prepare
        [JsonProperty]
        public string PostPermlink => $"@{Author}/{Permlink}";

        [JsonProperty("username")]
        [Required(ErrorMessage = nameof(LocalizationKeys.EmptyLogin))]
        public string Author { get; set; }

        [JsonProperty]
        [MaxLength(TagLimit, ErrorMessage = nameof(LocalizationKeys.TagLimitError))]
        public string[] Tags
        {
            get { return _tags; }
            set
            {
                if (value != null)
                    OperationHelper.PrepareTags(value);
                _tags = value;
            }
        }

        [JsonProperty]
        public bool ShowFooter { get; }

        [JsonProperty]
        [Required(ErrorMessage = nameof(LocalizationKeys.EmptyFileField))]

        public MediaModel[] Media { get; set; }

        [JsonProperty]
        [Required(ErrorMessage = nameof(LocalizationKeys.EmptyTitleField))]
        public string Title { get; set; }

        public bool IsEditMode { get; }

        public PreparePostModel(UserInfo user) : base(user)
        {
            ShowFooter = user.ShowFooter;
            Author = user.Login;
            IsEditMode = false;
        }

        public PreparePostModel(UserInfo user, string permlink) : base(user)
        {
            ShowFooter = user.ShowFooter;
            Author = user.Login;
            _permlink = permlink;
            IsEditMode = true;
        }
    }
}