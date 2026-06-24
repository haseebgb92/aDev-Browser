using System.Collections.ObjectModel;
using DevBrowser.Models;

namespace DevBrowser.Tabs
{
    public class TabManager
    {
        public ObservableCollection<TabModel> Tabs { get; } = new ObservableCollection<TabModel>();
        
        private TabModel? _selectedTab;
        public TabModel? SelectedTab
        {
            get => _selectedTab;
            set => _selectedTab = value;
        }

        public void AddTab(TabModel tab)
        {
            Tabs.Add(tab);
            SelectedTab = tab;
        }

        public void RemoveTab(TabModel tab)
        {
            Tabs.Remove(tab);
            if (SelectedTab == tab)
            {
                SelectedTab = Tabs.Count > 0 ? Tabs[Tabs.Count - 1] : null;
            }
        }
    }
}
