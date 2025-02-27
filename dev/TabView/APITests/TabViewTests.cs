﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MUXControlsTestApp.Utilities;
using System;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Common;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Provider;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Controls;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using Symbol = Windows.UI.Xaml.Controls.Symbol;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{

    [TestClass]
    public class TabViewTests : ApiTestBase
    {
        [TestMethod]
        public void VerifyCompactTabWidthVisualStates_ItemsMode()
        {
            VerifyCompactTabWidthVisualStates();
        }

        [TestMethod]
        public void VerifyCompactTabWidthVisualStates_ItemsSourceMode()
        {
            VerifyCompactTabWidthVisualStates(isItemsSourceMode: true);           
        }

        private void VerifyCompactTabWidthVisualStates(bool isItemsSourceMode = false)
        {
            TabView tabView = null;
            RunOnUIThread.Execute(() =>
            {
                tabView = new TabView();
                SetupTabViewItems();

                Log.Comment("Set TabWidthMode to compact");
                tabView.TabWidthMode = TabViewWidthMode.Compact;

                Log.Comment("Select a tab. In TabWidthMode compact, a selected tab will not be in compact state. We verify that behavior in this test");
                tabView.SelectedIndex = 0;

                Content = tabView;
            });

            IdleSynchronizer.Wait();

            RunOnUIThread.Execute(() =>
            {
                Log.Comment("Verify a selected tab exists");
                VerifySelectedItem("Tab 0");

                Log.Comment("Verify the created TabView displays every tab in compact mode");
                VerifyTabWidthVisualStates(tabView, tabView.TabItems, true);

                Log.Comment("Verify that adding a new item creates a tab in compact mode");
                AddItem("Tab 3");
                Content.UpdateLayout();

                VerifyTabWidthVisualStates(tabView, new List<object>() { tabView.TabItems[tabView.TabItems.Count - 1] }, true);

                Log.Comment("Change the TabWidthMode to non compact and verify that every tab is no longer in compact mode");
                tabView.TabWidthMode = TabViewWidthMode.Equal;
                Content.UpdateLayout();

                VerifyTabWidthVisualStates(tabView, tabView.TabItems, false);

                Log.Comment("Change the TabWidthMode to compact and verify that every tab is now in compact mode");
                tabView.TabWidthMode = TabViewWidthMode.Compact;
                Content.UpdateLayout();

                VerifyTabWidthVisualStates(tabView, tabView.TabItems, true);
            });

            void SetupTabViewItems()
            {
                if (isItemsSourceMode)
                {
                    var tabItemsSource = new ObservableCollection<string>() { "Tab 0", "Tab 1", "Tab 2" };
                    tabView.TabItemsSource = tabItemsSource;
                }
                else
                {
                    tabView.TabItems.Add(CreateTabViewItem("Tab 0", Symbol.Add));
                    tabView.TabItems.Add(CreateTabViewItem("Tab 1", Symbol.AddFriend));
                    tabView.TabItems.Add(CreateTabViewItem("Tab 2"));
                }
            }

            void VerifySelectedItem(string expectedHeader)
            {
                object selectedItemHeader = isItemsSourceMode
                    ? tabView.SelectedItem
                    : (tabView.SelectedItem as TabViewItem).Header;

                Verify.AreEqual(expectedHeader, selectedItemHeader);
            }

            void AddItem(string header)
            {
                if (isItemsSourceMode)
                {
                    ((ObservableCollection<string>)tabView.TabItemsSource).Add(header);
                }
                else
                {
                    tabView.TabItems.Add(CreateTabViewItem(header));
                }
            }
        }

        [TestMethod]
        public void VerifyTabViewUIABehavior()
        {
            RunOnUIThread.Execute(() =>
            {
                TabView tabView = new TabView();
                Content = tabView;

                tabView.TabItems.Add(CreateTabViewItem("Item 0", Symbol.Add));
                tabView.TabItems.Add(CreateTabViewItem("Item 1", Symbol.AddFriend));
                tabView.TabItems.Add(CreateTabViewItem("Item 2"));

                Content.UpdateLayout();

                var tabViewPeer = FrameworkElementAutomationPeer.CreatePeerForElement(tabView);
                Verify.IsNotNull(tabViewPeer);
                var tabViewSelectionPattern = tabViewPeer.GetPattern(PatternInterface.Selection);
                Verify.IsNotNull(tabViewSelectionPattern);
                var selectionProvider = tabViewSelectionPattern as ISelectionProvider;
                // Tab controls must require selection
                Verify.IsTrue(selectionProvider.IsSelectionRequired);
            });
        }

        [TestMethod]
        public void VerifyTabViewItemUIABehavior()
        {
            TabView tabView = null;

            TabViewItem tvi0 = null;
            TabViewItem tvi1 = null;
            TabViewItem tvi2 = null;
            RunOnUIThread.Execute(() =>
            {
                tabView = new TabView();
                Content = tabView;

                tvi0 = CreateTabViewItem("Item 0", Symbol.Add);
                tvi1 = CreateTabViewItem("Item 1", Symbol.AddFriend);
                tvi2 = CreateTabViewItem("Item 2");

                tabView.TabItems.Add(tvi0);
                tabView.TabItems.Add(tvi1);
                tabView.TabItems.Add(tvi2);

                tabView.SelectedIndex = 0;
                tabView.SelectedItem = tvi0;
                Content.UpdateLayout();
            });

            IdleSynchronizer.Wait();

            RunOnUIThread.Execute(() =>
            {
                var selectionItemProvider = GetProviderFromTVI(tvi0);
                Verify.IsTrue(selectionItemProvider.IsSelected, "Item should be selected");

                selectionItemProvider = GetProviderFromTVI(tvi1);
                Verify.IsFalse(selectionItemProvider.IsSelected, "Item should not be selected");

                Log.Comment("Change selection through automationpeer");
                selectionItemProvider.Select();
                Verify.IsTrue(selectionItemProvider.IsSelected, "Item should have been selected");

                selectionItemProvider = GetProviderFromTVI(tvi0);
                Verify.IsFalse(selectionItemProvider.IsSelected, "Item should not be selected anymore");

                Verify.IsNotNull(selectionItemProvider.SelectionContainer);
            });

            static ISelectionItemProvider GetProviderFromTVI(TabViewItem item)
            {
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(item);
                var provider = peer.GetPattern(PatternInterface.SelectionItem)
                                as ISelectionItemProvider;
                Verify.IsNotNull(provider);
                return provider;
            }
        }

        [TestMethod]
        public void VerifyPaneFooterSizingRespected()
        {
            foreach (var property in new DependencyProperty[] { FrameworkElement.MinWidthProperty, FrameworkElement.WidthProperty })
            {
                TabView tabView = null;
                Grid paneFooter = null;
                RunOnUIThread.Execute(() =>
                {
                    paneFooter = new Grid();
                    tabView = new TabView();

                    var container = new Grid() {
                        MaxWidth = 600
                    };
                    container.Children.Add(tabView);

                    tabView.TabItems.Add(new TabViewItem());
                    tabView.TabItems.Add(new TabViewItem());
                    tabView.TabItems.Add(new TabViewItem());
                    tabView.TabItems.Add(new TabViewItem());

                    tabView.TabStripFooter = paneFooter;
                    tabView.UpdateLayout();
                    tabView.HorizontalAlignment = HorizontalAlignment.Stretch;
                    tabView.Width = 600;

                    container.UpdateLayout();
                    Content = container;
                    paneFooter.UpdateLayout();
                });

                IdleSynchronizer.Wait();

                RunOnUIThread.Execute(() =>
                {
                    Verify.IsTrue(Math.Abs(600 - tabView.ActualWidth) < 1);
                    paneFooter.SetValue(property, 300);
                });
                IdleSynchronizer.Wait();

                RunOnUIThread.Execute(() =>
                {
                    var footerPresenter = VisualTreeHelper.GetParent(paneFooter) as FrameworkElement;
                    Verify.IsTrue(Math.Abs(300 - footerPresenter.ActualWidth) < 1);
                });
            }
        }



        private void VerifyTabViewWithoutTabsDoesNotCrash()
        {
            RunOnUIThread.Execute(() =>
            {
                TabView tabView = new TabView();
                Content = tabView;

                // Creating a TabView without tabs should not crash the app.
                Content.UpdateLayout();

                var tabItemsSource = new ObservableCollection<string>() { "Tab 1", "Tab 2" };
                tabView.TabItemsSource = tabItemsSource;

                // Clearing the ItemsSource should not crash the app.
                Log.Comment("Clear the specified tab items source");
                tabItemsSource.Clear();
            });       
        }

        [TestMethod]
        public void TabViewItemBackgroundTest()
        {
            TabView tabView = null;
            TabViewItem tvi1 = null;
            TabViewItem tvi2 = null;
            RunOnUIThread.Execute(() =>
            {
                tabView = new TabView();

                tvi1 = CreateTabViewItem("Tab1", Symbol.Home);
                tvi2 = CreateTabViewItem("Tab2", Symbol.Document);
                tabView.TabItems.Add(tvi1);
                tabView.TabItems.Add(tvi2);

                Content = tabView;
                Content.UpdateLayout();
            });

            IdleSynchronizer.Wait();

            RunOnUIThread.Execute(() =>
            {
                var headerBackground = Application.Current.Resources["TabViewItemHeaderBackground"] as Brush;
                var tabContainer = tvi2.FindVisualChildByName("TabContainer") as Grid;

                // Verify that the TabViewItem we use for Background API testing is unselected.
                Verify.IsFalse(tvi2.IsSelected, "TabViewItem should have been unselected");

                Log.Comment("Verify that the default background brush is set by the [TabViewItemHeaderBackground] theme resource.");
                Verify.IsTrue(ReferenceEquals(tvi2.Background, headerBackground), "TabViewItem's default header background brush should have been [TabViewItemHeaderBackground]");
                Verify.IsTrue(ReferenceEquals(tabContainer.Background, headerBackground), "TabViewItem's [TabContainer] background brush should have been [TabViewItemHeaderBackground]");

                var testBrush = new SolidColorBrush(Colors.Blue);
                Verify.IsFalse(ReferenceEquals(testBrush, headerBackground), "Our test brush should have not been [TabViewItemHeaderBackground]");

                Log.Comment("Set the TabViewItem's background using the Background API.");
                tvi2.Background = testBrush;

                // Verify that the background brushes have been updated correctly.
                Verify.IsTrue(ReferenceEquals(tvi2.Background, testBrush), "TabViewItem's Background brush should have been [testBrush]");
                Verify.IsTrue(ReferenceEquals(tabContainer.Background, testBrush), "TabViewItem's [TabContainer] background brush should have been [testBrush]");
            });
        }
        
        [TestMethod]
        public void TabViewItemHeaderTest()
        {
            TabViewItem tvi0 = null;
            TabViewItem tvi1 = null;
            TabViewItem tvi2 = null;
            RunOnUIThread.Execute(() =>
            {
                var tabView = new TabView();

                tvi0 = CreateTabViewItem(null, "tab0Content");
                tvi1 = CreateTabViewItem("", "tab1Content");
                tvi2 = CreateTabViewItem("tab2", "tab2Content");

                tabView.TabItems.Add(tvi0);
                tabView.TabItems.Add(tvi1);
                tabView.TabItems.Add(tvi2);

                Content = tabView;
                Content.UpdateLayout();
            });

            IdleSynchronizer.Wait();

            RunOnUIThread.Execute(() =>
            {
                // Verify headers
                var headerContentPresenter1 = VisualTreeUtils.FindVisualChildByName(tvi0, "ContentPresenter") as ContentPresenter;
                var headerContentPresenter2 = VisualTreeUtils.FindVisualChildByName(tvi1, "ContentPresenter") as ContentPresenter;
                var headerContentPresenter3 = VisualTreeUtils.FindVisualChildByName(tvi2, "ContentPresenter") as ContentPresenter;

                Verify.AreEqual(null, headerContentPresenter1.Content, "tvi0's header should have been [null]");
                Verify.AreEqual("", headerContentPresenter2.Content, "tvi1's header should have been the empty string");
                Verify.AreEqual("tab2", headerContentPresenter3.Content, "tvi2's header should have been \"tab2\"");

                // Verify ToolTips
                var toolTip0 = ToolTipService.GetToolTip(tvi0) as ToolTip;
                var toolTip1 = ToolTipService.GetToolTip(tvi1) as ToolTip;
                var toolTip2 = ToolTipService.GetToolTip(tvi2) as ToolTip;

                bool testCondition = toolTip0.IsEnabled == false && toolTip0.Content == null;
                Verify.IsTrue(testCondition, "tvi0's ToolTip should have been disabled with [null] as content");

                testCondition = toolTip1.IsEnabled == false && toolTip1.Content == null;
                Verify.IsTrue(testCondition, "tvi1's ToolTip should have been disabled with [null] as content");

                testCondition = toolTip2.IsEnabled == true && toolTip2.Content is string s && s == "tab2";
                Verify.IsTrue(testCondition, "tvi2's ToolTip should have been enabled with \"tab2\" as content");
            });
        }
          
        [TestMethod]
        public void TabViewItemForegroundTest()
        {
            TabView tabView = null;
            TabViewItem tvi1 = null;
            TabViewItem tvi2 = null;
            RunOnUIThread.Execute(() =>
            {
                tabView = new TabView();

                tvi1 = CreateTabViewItem("Tab1", Symbol.Home);
                tvi2 = CreateTabViewItem("Tab2", Symbol.Document);

                tabView.TabItems.Add(tvi1);
                tabView.TabItems.Add(tvi2);

                Content = tabView;
                Content.UpdateLayout();
            });

            IdleSynchronizer.Wait();

            RunOnUIThread.Execute(() =>
            {
                var iconForeground = Application.Current.Resources["TabViewItemIconForeground"] as Brush;
                var headerForeground = Application.Current.Resources["TabViewItemHeaderForeground"] as Brush;

                var iconControl = tvi2.FindVisualChildByName("IconControl") as ContentControl;
                var headerPresenter = tvi2.FindVisualChildByName("ContentPresenter") as ContentPresenter;

                // Verify that the TabViewItem we use for Foreground API testing is unselected.
                Verify.IsFalse(tvi2.IsSelected, "TabViewItem should have been unselected");

                Log.Comment("Verify that theme resource brushes are used when no foreground was set using the Foreground API.");
                Verify.IsTrue(ReferenceEquals(iconControl.Foreground, iconForeground), "TabViewItem's icon foreground brush should have been [TabViewItemIconForeground]");
                Verify.IsTrue(ReferenceEquals(headerPresenter.Foreground, headerForeground), "TabViewItem's header foreground brush should have been [TabViewItemHeaderForeground]");

                var testBrush = new SolidColorBrush(Colors.Blue);
                Verify.IsFalse(ReferenceEquals(testBrush, iconForeground), "Our test brush should have not been [TabViewItemIconForeground]");
                Verify.IsFalse(ReferenceEquals(testBrush, headerForeground), "Our test brush should have not been [TabViewItemHeaderForeground]");

                Log.Comment("Set the TabViewItem's foreground (icon + header) using the Foreground API.");
                tvi2.Foreground = testBrush;

                Verify.IsTrue(ReferenceEquals(tvi2.Foreground, testBrush), "TabViewItem's Foreground brush should have been [testBrush]");

                // Verify that the icon and header foreground brushes have been updated correctly.
                Verify.IsTrue(ReferenceEquals(iconControl.Foreground, testBrush), "TabViewItem's icon foreground brush should have been [testBrush]");
                Verify.IsTrue(ReferenceEquals(headerPresenter.Foreground, testBrush), "TabViewItem's header foreground brush should have been [testBrush]");

                Log.Comment("Unset TabViewItem.Foreground to apply the theme resource brushes again.");
                tvi2.ClearValue(Control.ForegroundProperty);

                Verify.IsTrue(ReferenceEquals(iconControl.Foreground, iconForeground), "TabViewItem's icon foreground brush should have been [TabViewItemIconForeground]");
                Verify.IsTrue(ReferenceEquals(headerPresenter.Foreground, headerForeground), "TabViewItem's header foreground brush should have been [TabViewItemHeaderForeground]");
            });
        }

        private static void VerifyTabWidthVisualStates(TabView tabView, IList<object> items, bool isCompact)
        {
            var listView = VisualTreeUtils.FindVisualChildByName(tabView, "TabListView") as TabViewListView;

            foreach (var item in items)
            {
                var tabItem = item is TabViewItem
                    ? (TabViewItem)item
                    : listView.ContainerFromItem(item) as TabViewItem;

                var rootGrid = VisualTreeHelper.GetChild(tabItem, 0) as FrameworkElement;

                foreach (var group in VisualStateManager.GetVisualStateGroups(rootGrid))
                {
                    if (group.Name == "TabWidthModes")
                    {
                        if (tabItem.IsSelected || !isCompact)
                        {
                            Verify.AreEqual("StandardWidth", group.CurrentState.Name, "Verify that this tab item is rendering in standard width");
                        }
                        else
                        {
                            Verify.AreEqual("Compact", group.CurrentState.Name, "Verify that this tab item is rendering in compact width");
                        }
                    }
                }
            }
        }

        private static TabViewItem CreateTabViewItem(string name, Symbol icon, bool closable = true, bool enabled = true)
        {
            var tabViewItem = new TabViewItem();

            tabViewItem.Header = name;
            tabViewItem.IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource() { Symbol = icon };
            tabViewItem.IsClosable = closable;
            tabViewItem.IsEnabled = enabled;

            return tabViewItem;
        }

        private static TabViewItem CreateTabViewItem(string name, object content, bool closable = true, bool enabled = true)
        {
            var tabViewItem = new TabViewItem();

            tabViewItem.Header = name;
            tabViewItem.Content = content;
            tabViewItem.IsClosable = closable;
            tabViewItem.IsEnabled = enabled;

            return tabViewItem;
        }

        private static TabViewItem CreateTabViewItem(string name, bool closable = true, bool enabled = true)
        {
            var tabViewItem = new TabViewItem();

            tabViewItem.Header = name;
            tabViewItem.IsClosable = closable;
            tabViewItem.IsEnabled = enabled;

            return tabViewItem;
        }
    }
}
