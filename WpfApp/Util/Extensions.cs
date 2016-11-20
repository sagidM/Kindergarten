using System.ComponentModel;

namespace WpfApp.Util
{
    public static class Extensions
    {
        public static void TryRefreshFilter(this ICollectionView collectionView)
        {
            if (collectionView?.Filter != null)
                collectionView.Filter = collectionView.Filter;
        }
    }
}