// Copyright (c) DotSpatial Team. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

using DotSpatial.Controls.Header;
using System;
using System.ComponentModel.Composition;
using System.Runtime.Remoting.Messaging;
using System.Windows.Forms;

namespace DemoMap
{
    /// <summary>
    /// This is the main window of the DemoMap program.
    /// </summary>
    public partial class MainForm : Form
    {
        [Export("Shell", typeof(ContainerControl))]
        private static ContainerControl shell;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainForm"/> class.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            if (DesignMode) return;
            shell = this;
            appManager.LoadExtensions();
        }

        private void MainForm_Load(object sender, System.EventArgs e)
        {
            var header = appManager.HeaderControl;

            const string SampleMenuKey = "kStuff";
            // Root menu
            header.Add(new RootItem(SampleMenuKey, "Stuff"));

            // Add some child menus
            var theKey = "Replace Words in Programs";
            header.Add(new SimpleActionItem(SampleMenuKey, theKey, OnMenuClickEventHandler) { Enabled = true });
            header.Add(new MenuContainerItem(SampleMenuKey, "container1", "container1"));
            header.Add(new MenuContainerItem(SampleMenuKey, "container1", "container2", "container2"));
            header.Add(new MenuContainerItem(SampleMenuKey, "container1_container2", "container3", "container3"));
            header.Add(new SimpleActionItem(SampleMenuKey, "container1_container2", "action2", OnMenuClickEventHandler) { Enabled = true });

        }

        private void OnMenuClickEventHandler(object sender, EventArgs e)
        {
            var act = ((SimpleActionItem)sender).Caption;

            MessageBox.Show($"{act} is activated.");
        }
    }
}