﻿using Android.Support.V7.Widget;

namespace Steepshot.Utils
{
    public sealed class ListItemDecoration : RecyclerView.ItemDecoration
    {
        private readonly int _offset;

        public ListItemDecoration(int offset)
        {
            _offset = offset;
        }

        public override void GetItemOffsets(Android.Graphics.Rect outRect, Android.Views.View view, RecyclerView parent, RecyclerView.State state)
        {
            outRect.Set(_offset, 0, 0, 0);
        }
    }
}
