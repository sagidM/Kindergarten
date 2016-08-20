using System.Collections;
using System.Collections.Generic;
using System.Windows.Markup;

namespace WpfApp.Framework
{
    [ContentProperty(nameof(Pairs))]
    public class ViewViewModelPairs
    {
        public ICollection<ViewViewModelPair> Pairs { get; set; }
    }
}