﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoreGraphics;
using FFImageLoading;
using Foundation;
using Sweetshot.Library.Models.Common;
using Sweetshot.Library.Models.Requests;
using Sweetshot.Library.Models.Responses;
using UIKit;

namespace Steepshot.iOS
{
    public partial class ProfileViewController : BaseViewController
    {
        protected ProfileViewController(IntPtr handle) : base(handle) { }

        public ProfileViewController()
        {
        }

        private UserProfileResponse userData;
        public string Username = User.Login;
        private ProfileCollectionViewSource collectionViewSource = new ProfileCollectionViewSource();
        private List<Post> photosList = new List<Post>();
        private string _offsetUrl;
        private bool _hasItems = true;
        private UIRefreshControl RefreshControl;
        private bool _isPostsLoading;
        private ProfileHeaderViewController _profileHeader;
        private CollectionViewFlowDelegate gridDelegate;
        private int _lastRow;
        private const int _limit = 40;
        private UINavigationController navController;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            if (TabBarController != null)
                TabBarController.NavigationController.NavigationBarHidden = true;
            if (Username != User.Login)
                topViewHeight.Constant = 0;

            navController = TabBarController != null ? TabBarController.NavigationController : NavigationController;
            collectionViewSource.PhotoList = photosList;
            collectionViewSource.Voted += (vote, postUri, success) => Vote(vote, postUri, success);
            collectionViewSource.Flagged += (vote, url, action) => Flagged(vote, url, action);
            collectionViewSource.GoToComments += (postUrl) =>
            {
                var myViewController = new CommentsViewController();
                myViewController.PostUrl = postUrl;
                navController.PushViewController(myViewController, true);
            };
            collectionViewSource.GoToVoters += (postUrl) =>
            {
                var myViewController = new VotersViewController();
                myViewController.PostUrl = postUrl;
                NavigationController.PushViewController(myViewController, true);
            };
            collectionViewSource.ImagePreview += PreviewPhoto;

            collectionView.RegisterClassForCell(typeof(PhotoCollectionViewCell), nameof(PhotoCollectionViewCell));
            collectionView.RegisterNibForCell(UINib.FromName(nameof(PhotoCollectionViewCell), NSBundle.MainBundle), nameof(PhotoCollectionViewCell));
            collectionView.RegisterClassForCell(typeof(FeedCollectionViewCell), nameof(FeedCollectionViewCell));
            collectionView.RegisterNibForCell(UINib.FromName(nameof(FeedCollectionViewCell), NSBundle.MainBundle), nameof(FeedCollectionViewCell));
            //collectioViewFlowLayout.EstimatedItemSize = Constants.CellSize;
            collectionView.Source = collectionViewSource;

            gridDelegate = new CollectionViewFlowDelegate((indexPath) =>
            {
                var collectionCell = (PhotoCollectionViewCell)collectionView.CellForItem(indexPath);
                PreviewPhoto(collectionCell.Image, collectionCell.ImageUrl);
            },
            () =>
            {
                if (collectionView.IndexPathsForVisibleItems.Count() != 0)
                {
                    var newlastRow = collectionView.IndexPathsForVisibleItems.Max(c => c.Row) + 2;

                    if (collectionViewSource.PhotoList.Count <= _lastRow && _hasItems && !RefreshControl.Refreshing)
                        GetUserPosts();
                    _lastRow = newlastRow;
                }
            }, collectionViewSource.FeedStrings);

            collectionView.Delegate = gridDelegate;

            _profileHeader = new ProfileHeaderViewController(ProfileHeaderLoaded);
            collectionView.ContentInset = new UIEdgeInsets(300, 0, 0, 0);
            collectionView.AddSubview(_profileHeader.View);

            RefreshControl = new UIRefreshControl();
            RefreshControl.ValueChanged += RefreshControl_ValueChanged;
            collectionView.Add(RefreshControl);

            GetUserInfo();
            GetUserPosts();
        }

        async void RefreshControl_ValueChanged(object sender, EventArgs e)
        {
            await RefreshPage();
            RefreshControl.EndRefreshing();
        }

        public override void ViewWillAppear(bool animated)
        {
            if (Username == User.Login)
            {
                NavigationController.SetNavigationBarHidden(true, false);
                if (TabBarController != null)
                    TabBarController.NavigationController.SetNavigationBarHidden(true, false);
            }
            base.ViewWillAppear(animated);
        }

        private void ProfileHeaderLoaded()
        {
            _profileHeader.SwitchButton.TouchDown += (sender, e) =>
            {
                if (!collectionViewSource.IsGrid)
                {
                    //collectioViewFlowLayout.EstimatedItemSize = Constants.CellSize;
                    _profileHeader.SwitchButton.SetImage(UIImage.FromFile("list.png"), UIControlState.Normal);
                }
                else
                {
                    //collectioViewFlowLayout.EstimatedItemSize = new CGSize(UIScreen.MainScreen.Bounds.Width, 485);
                    _profileHeader.SwitchButton.SetImage(UIImage.FromFile("grid.png"), UIControlState.Normal);

                }
                gridDelegate.IsGrid = collectionViewSource.IsGrid = !collectionViewSource.IsGrid;
                collectionView.ReloadData();
            };

            _profileHeader.FollowButton.TouchDown += (object sender, EventArgs e) =>
            {
                Follow();
            };

            _profileHeader.SettingsButton.TouchDown += (sender, e) =>
            {
                var myViewController = new SettingsViewController();
                TabBarController.NavigationController.PushViewController(myViewController, true);
            };

            _profileHeader.FollowingButton.TouchDown += (sender, e) =>
            {
                var myViewController = new FollowViewController();
                myViewController.Username = Username;
                myViewController.FriendsType = FriendsType.Following;
                NavigationController.PushViewController(myViewController, true);
            };

            _profileHeader.FollowersButton.TouchDown += (sender, e) =>
            {
                var myViewController = new FollowViewController();
                myViewController.Username = Username;
                myViewController.FriendsType = FriendsType.Followers;
                NavigationController.PushViewController(myViewController, true);
            };
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            if (ShouldProfileUpdate)
            {
                RefreshPage();
                ShouldProfileUpdate = false;
            }
        }

        private async Task RefreshPage()
        {
            photosList.Clear();
            collectionViewSource.FeedStrings.Clear();
            _hasItems = true;
            GetUserInfo();
            await GetUserPosts();
        }

        private void PreviewPhoto(UIImage image, string url)
        {
            var myViewController = new ImagePreviewViewController();
            myViewController.imageForPreview = image;
            myViewController.ImageUrl = url;
            navController.PushViewController(myViewController, true);
        }

        private async Task GetUserInfo()
        {
            errorMessage.Hidden = true;
            try
            {
                var req = new UserProfileRequest(Username) { SessionId = User.SessionId };
                var response = await Api.GetUserProfile(req);
                if (response.Success)
                {
                    userData = response.Result;
                    _profileHeader.Username.Text = !string.IsNullOrEmpty(userData.Name) ? userData.Name : userData.Username;
                    var culture = new CultureInfo("en-US");
                    _profileHeader.Date.Text = $"Joined {userData.Created.ToString("Y", culture)}";
                    if (!string.IsNullOrEmpty(userData.Location))
                        _profileHeader.Location.Text = userData.Location;
                    if (!string.IsNullOrEmpty(userData.About))
                        _profileHeader.DescriptionLabel.Text = userData.About;

                    if (!string.IsNullOrEmpty(userData.ProfileImage))
                        ImageService.Instance.LoadUrl(userData.ProfileImage, TimeSpan.FromDays(30))
                                             .Retry(2, 200)
                                             .FadeAnimation(false, false, 0)
                                             .DownSample(width: (int)_profileHeader.Avatar.Frame.Width)
                                             .Into(_profileHeader.Avatar);
                    else
                        _profileHeader.Avatar.Image = UIImage.FromBundle("ic_user_placeholder");

                    _profileHeader.Balance.SetTitle($"{userData.EstimatedBalance.ToString()}{Currency}", UIControlState.Normal);
                    _profileHeader.SettingsButton.Hidden = Username != User.Login;

                    var buttonsAttributes = new UIStringAttributes
                    {
                        Font = Constants.Bold12,
                        ForegroundColor = UIColor.FromRGB(51, 51, 51),
                        ParagraphStyle = new NSMutableParagraphStyle() { LineSpacing = 5, Alignment = UITextAlignment.Center }
                    };

                    var textAttributes = new UIStringAttributes
                    {
                        Font = Constants.Bold9,
                        ForegroundColor = UIColor.FromRGB(153, 153, 153),
                        ParagraphStyle = new NSMutableParagraphStyle() { LineSpacing = 5, Alignment = UITextAlignment.Center }
                    };

                    NSMutableAttributedString photosString = new NSMutableAttributedString();
                    photosString.Append(new NSAttributedString(userData.PostCount.ToString(), buttonsAttributes));
                    photosString.Append(new NSAttributedString(Environment.NewLine));
                    photosString.Append(new NSAttributedString("PHOTOS", textAttributes));

                    _profileHeader.PhotosButton.TitleLabel.LineBreakMode = UILineBreakMode.WordWrap;
                    _profileHeader.PhotosButton.TitleLabel.TextAlignment = UITextAlignment.Center;
                    _profileHeader.PhotosButton.SetAttributedTitle(photosString, UIControlState.Normal);

                    NSMutableAttributedString followingString = new NSMutableAttributedString();
                    followingString.Append(new NSAttributedString(userData.FollowingCount.ToString(), buttonsAttributes));
                    followingString.Append(new NSAttributedString(Environment.NewLine));
                    followingString.Append(new NSAttributedString("FOLLOWING", textAttributes));

                    _profileHeader.FollowingButton.TitleLabel.LineBreakMode = UILineBreakMode.WordWrap;
                    _profileHeader.FollowingButton.TitleLabel.TextAlignment = UITextAlignment.Center;
                    _profileHeader.FollowingButton.SetAttributedTitle(followingString, UIControlState.Normal);

                    NSMutableAttributedString followersString = new NSMutableAttributedString();
                    followersString.Append(new NSAttributedString(userData.FollowersCount.ToString(), buttonsAttributes));
                    followersString.Append(new NSAttributedString(Environment.NewLine));
                    followersString.Append(new NSAttributedString("FOLLOWERS", textAttributes));

                    _profileHeader.FollowersButton.TitleLabel.LineBreakMode = UILineBreakMode.WordWrap;
                    _profileHeader.FollowersButton.TitleLabel.TextAlignment = UITextAlignment.Center;
                    _profileHeader.FollowersButton.SetAttributedTitle(followersString, UIControlState.Normal);

                    ToogleFollowButton();

                    if (!RefreshControl.Refreshing)
                    {
                        _profileHeader.View.SetNeedsLayout();
                        _profileHeader.View.LayoutIfNeeded();
                        var size = _profileHeader.View.SystemLayoutSizeFittingSize(new CGSize(UIScreen.MainScreen.Bounds.Width, 300));

                        _profileHeader.View.Frame = new CGRect(0, -size.Height, UIScreen.MainScreen.Bounds.Width, size.Height);
                        collectionView.ContentInset = new UIEdgeInsets(size.Height, 0, 0, 0);
                        collectionView.Hidden = false;
                    }
                }
                else
                {
                    Reporter.SendCrash(response.Errors[0], User.Login, AppVersion);
                }
            }
            catch (Exception ex)
            {
                errorMessage.Hidden = false;
                Reporter.SendCrash(ex, User.Login, AppVersion);
            }
            finally
            {
                loading.StopAnimating();
            }
        }

        public async Task GetUserPosts()
        {
            if (_isPostsLoading || !_hasItems)
                return;
            _isPostsLoading = true;
            try
            {
                var req = new UserPostsRequest(Username)
                {
                    Limit = _limit,
                    Offset = photosList.Count == 0 ? "0" : _offsetUrl,
                    SessionId = User.SessionId
                };
                var response = await Api.GetUserPosts(req);
                if (response.Success)
                {
                    if (response?.Result?.Results?.Count != 0)
                    {
                        response?.Result?.Results?.FilterHided();
                        var lastItem = response?.Result?.Results?.Last();
                        _offsetUrl = lastItem?.Url;

                        if (response?.Result?.Results?.Count < _limit / 2)
                            _hasItems = false;
                        else
                            response.Result.Results.Remove(lastItem);

                        foreach (var r in response?.Result?.Results)
                        {
                            var at = new NSMutableAttributedString();
                            at.Append(new NSAttributedString(r.Author, Constants.NicknameAttribute));
                            at.Append(new NSAttributedString($" {r.Title}"));
                            collectionViewSource.FeedStrings.Add(at);
                        }
                        photosList.AddRange(response?.Result?.Results);
                    }
                    collectionView.ReloadData();
                    collectionView.CollectionViewLayout.InvalidateLayout();
                }
                else
                {
                    Reporter.SendCrash("Profile page get posts erorr: " + response.Errors[0], BaseViewController.User.Login, BaseViewController.AppVersion);
                    ShowAlert(response.Errors[0]);
                }
            }
            catch (Exception ex)
            {
                Reporter.SendCrash(ex, User.Login, AppVersion);
            }
            finally
            {
                _isPostsLoading = false;
            }
        }


        private async Task Vote(bool vote, string postUri, Action<string, OperationResult<VoteResponse>> success)
        {
            if (!User.IsAuthenticated)
            {
                LoginTapped();
                return;
            }

            try
            {
                if (User.SessionId == null)
                {
                    var myViewController = new LoginViewController();
                    NavigationController.PushViewController(myViewController, true);
                    return;
                }

                var voteRequest = new VoteRequest(User.SessionId, vote, postUri);
                var voteResponse = await Api.Vote(voteRequest);
                if (voteResponse.Success)
                {
                    var u = photosList.FirstOrDefault(p => p.Url == postUri);
                    if (u != null)
                    {
                        u.Vote = vote;
                        if (vote)
                        {
                            u.Flag = false;
                            if (u.NetVotes == -1)
                                u.NetVotes = 1;
                            else
                                u.NetVotes++;
                        }
                        else
                            u.NetVotes--;
                    }
                }
                else
                {
                    Reporter.SendCrash("Profile page vote erorr: " + voteResponse.Errors[0], BaseViewController.User.Login, BaseViewController.AppVersion);
                    ShowAlert(voteResponse.Errors[0]);
                }
                success.Invoke(postUri, voteResponse);
            }
            catch (Exception ex)
            {
                Reporter.SendCrash(ex, User.Login, AppVersion);
            }
        }

        private void Flagged(bool vote, string postUrl, Action<string, OperationResult<FlagResponse>> action)
        {
            if (!User.IsAuthenticated)
            {
                LoginTapped();
                return;
            }
            UIAlertController actionSheetAlert = UIAlertController.Create(null, null, UIAlertControllerStyle.ActionSheet);
            actionSheetAlert.AddAction(UIAlertAction.Create("Flag photo", UIAlertActionStyle.Default, (obj) => FlagPhoto(vote, postUrl, action)));
            actionSheetAlert.AddAction(UIAlertAction.Create("Hide photo", UIAlertActionStyle.Default, (obj) => HidePhoto(postUrl)));
            actionSheetAlert.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, (obj) => action.Invoke(postUrl, new OperationResult<FlagResponse>())));
            this.PresentViewController(actionSheetAlert, true, null);
        }

        private void HidePhoto(string url)
        {
            try
            {
                User.Postblacklist.Add(url);
                User.Save();
                var postToHide = collectionViewSource.PhotoList.FirstOrDefault(p => p.Url == url);
                if (postToHide != null)
                {
                    var postIndex = collectionViewSource.PhotoList.IndexOf(postToHide);
                    collectionViewSource.PhotoList.Remove(postToHide);
                    collectionViewSource.FeedStrings.RemoveAt(postIndex);
                    collectionView.ReloadData();
                    collectionView.CollectionViewLayout.InvalidateLayout();
                }
            }
            catch (Exception ex)
            {
                Reporter.SendCrash(ex, User.Login, AppVersion);
            }
        }

        private async Task FlagPhoto(bool vote, string postUrl, Action<string, OperationResult<FlagResponse>> action)
        {
            try
            {
                var flagRequest = new FlagRequest(User.SessionId, vote, postUrl);
                var flagResponse = await Api.Flag(flagRequest);
                if (flagResponse.Success)
                {
                    var u = collectionViewSource.PhotoList.FirstOrDefault(p => p.Url == postUrl);
                    if (u != null)
                    {
                        u.Flag = flagResponse.Result.IsFlagged;
                        if (flagResponse.Result.IsFlagged)
                        {
                            if (u.Vote)
                                if (u.NetVotes == 1)
                                    u.NetVotes = -1;
                                else
                                    u.NetVotes--;
                            u.Vote = false;
                        }
                    }
                }
                else
                {
                    ShowAlert(flagResponse.Errors[0]);
                }
                action.Invoke(postUrl, flagResponse);
            }
            catch (Exception ex)
            {
                Reporter.SendCrash(ex, User.Login, AppVersion);
            }
        }

        public async Task Follow()
        {
            var request = new FollowRequest(User.SessionId, (userData.HasFollowed == 0) ? FollowType.Follow : FollowType.UnFollow, userData.Username);
            var resp = await Api.Follow(request);
            if (resp.Errors.Count == 0)
            {
                userData.HasFollowed = (resp.Result.IsFollowed) ? 1 : 0;
                ToogleFollowButton();
            }
            else
                Reporter.SendCrash("Profile page follow error: " + resp.Errors[0], BaseViewController.User.Login, BaseViewController.AppVersion);

        }

        void LoginTapped()
        {
            var myViewController = new PreLoginViewController();
            NavigationController.PushViewController(myViewController, true);
        }

        private void ToogleFollowButton()
        {
            if (!User.IsAuthenticated || Username == User.Login || Convert.ToBoolean(userData.HasFollowed))
            {
                _profileHeader.FollowButtonWidth.Constant = 0;
                _profileHeader.FollowButtonMargin.Constant = 0;
            }
        }
    }
}
