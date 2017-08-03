﻿using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Widget;
using Com.Lilarcor.Cheeseknife;
using Steepshot.Adapter;
using Steepshot.Base;
using Steepshot.Core.Models.Responses;
using Steepshot.Presenter;

using Steepshot.Utils;
using Steepshot.View;

namespace Steepshot.Activity
{
	[Activity(Label = "TagsActivity",ScreenOrientation =Android.Content.PM.ScreenOrientation.Portrait, WindowSoftInputMode=Android.Views.SoftInput.AdjustNothing)]
	public class TagsActivity : BaseActivity, ITagsView
	{
		TagsPresenter _presenter;
#pragma warning disable 0649, 4014
		[InjectView(Resource.Id.ic_close)] ImageButton _close;
		[InjectView(Resource.Id.search_box)] EditText _searchBox;
		[InjectView(Resource.Id.tags_list)] RecyclerView _tagsList;
		[InjectView(Resource.Id.tag_container)] TagLayout _tagLayout;
		[InjectView(Resource.Id.scroll)] ScrollView _scroll;
		[InjectView(Resource.Id.add_custom_tag)] Button _addCustomTagButton;
#pragma warning restore 0649

        [InjectOnClick(Resource.Id.btn_post)]
        public void PostTags(object sender, EventArgs e)
        {
            if (_selectedCategories.Count > 0)
            {
                Intent returnIntent = new Intent();
                Bundle b = new Bundle();
                b.PutStringArray("TAGS", _selectedCategories.Select(o => o.Name).ToArray<string>());
                returnIntent.PutExtra("TAGS", b);
                SetResult(Result.Ok,returnIntent);
                Finish();
            }
        }

        [InjectOnClick(Resource.Id.add_custom_tag)]
		public void AddTag(object sender, EventArgs e)
		{
			var trimmedString = _searchBox.Text.Trim();
			if (string.IsNullOrEmpty(trimmedString))
			   return;
			AddTag(new SearchResult() { Name = trimmedString });
			_searchBox.Text = string.Empty;
		}

		TagsAdapter _adapter;

		List<SearchResult> _selectedCategories = new List<SearchResult>();

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			SetContentView(Resource.Layout.lyt_tags);

			Cheeseknife.Inject(this);

			_tagsList.SetLayoutManager(new LinearLayoutManager(this));
			_adapter = new TagsAdapter(this);
			_tagsList.SetAdapter(_adapter);

			_close.Click +=(sender,e)=> OnBackPressed();

			_searchBox.TextChanged += (sender, e) =>
			{
				if (_searchBox.Text.Length > 1)
				{
					_addCustomTagButton.Visibility = Android.Views.ViewStates.Visible;
					_presenter.SearchTags(_searchBox.Text).ContinueWith((arg) =>
					{
						_adapter.Reset(arg.Result.Result.Results);
						RunOnUiThread(() => _adapter.NotifyDataSetChanged());
					});
				}
				else
				{
					_addCustomTagButton.Visibility = Android.Views.ViewStates.Gone;
				}
			};


			_adapter.Click += Adapter_Click;

			_presenter.GetTopTags().ContinueWith((arg) => {
                _adapter.Reset(arg.Result.Result.Results);
                RunOnUiThread(() => _adapter.NotifyDataSetChanged());
            });
        }

		void Adapter_Click(int obj)
		{
			AddTag(_adapter.GetItem(obj));
		}

		public void AddTag(SearchResult s)
		{
			if (s.Name.Length > 1 && _selectedCategories.Count < 4 && _selectedCategories.Find((finded) => finded.Name.Equals(s.Name)) == null)
			{
				_selectedCategories.Add(s);
				FrameLayout add = (FrameLayout)LayoutInflater.Inflate(Resource.Layout.lyt_tag, null, false);
				var text = add.FindViewById<TextView>(Resource.Id.text);
				text.Text = s.Name;
				text.Click += (sender, e) => DeleteTag(text);
				_tagLayout.AddView(add);
				_scroll.RequestLayout();
			}
		}

		void DeleteTag(TextView t)
		{ 
			_selectedCategories.RemoveAt(_selectedCategories.FindIndex((obj) => obj.Name.Equals(t.Text)));
			_tagLayout.RemoveView((FrameLayout)t.Parent);
		}

		protected override void CreatePresenter()
		{
			_presenter = new TagsPresenter(this);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Cheeseknife.Reset(this);
		}
	}
}