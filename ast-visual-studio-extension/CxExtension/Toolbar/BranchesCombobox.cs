﻿using ast_visual_studio_extension.CxCli;
using ast_visual_studio_extension.CxCLI.Models;
using ast_visual_studio_extension.CxExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ast_visual_studio_extension.CxExtension.Toolbar
{
    internal class BranchesCombobox
    {
        private readonly ScansCombobox scansCombobox;

        private readonly CxToolbar cxToolbar;

        private bool firstLoad = true;

        public BranchesCombobox(CxToolbar cxToolbar, ScansCombobox scansCombobox)
        {
            this.cxToolbar = cxToolbar;
            this.scansCombobox = scansCombobox;
        }

        /// <summary>
        /// Populate Branches combobox
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public async Task LoadBranchesAsync(string projectId)
        {
            CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree);
            if (cxWrapper == null)
            {
                cxToolbar.BranchesCombo.Text = CxConstants.TOOLBAR_SELECT_BRANCH;
                return;
            }

            string errorMessage = string.Empty;

            List<string> branches = await Task.Run(() =>
            {
                try
                {
                    return cxWrapper.GetBranches(projectId);
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    return null;
                }
            });

            if (!string.IsNullOrEmpty(errorMessage))
            {
                cxToolbar.ResultsTree.Items.Clear();
                cxToolbar.ResultsTree.Items.Add(errorMessage);

                return;
            }

            cxToolbar.BranchesCombo.Items.Clear();

            for (int i = 0; i < branches.Count; i++)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem
                {
                    Content = branches[i]
                };

                cxToolbar.BranchesCombo.Items.Add(comboBoxItem);
            }

            cxToolbar.BranchesCombo.Text = CxConstants.TOOLBAR_SELECT_BRANCH;
            cxToolbar.EnableCombos(true);

            if(!string.IsNullOrEmpty(CxToolbar.currentBranch))
            {
                cxToolbar.BranchesCombo.SelectedIndex = CxUtils.GetItemIndexInCombo(CxToolbar.currentBranch, cxToolbar.BranchesCombo, Enums.ComboboxType.BRANCHES);
            }
            else
            {
                string branch = SettingsUtils.GetToolbarValue(cxToolbar.Package, SettingsUtils.branchProperty);

                if (!string.IsNullOrEmpty(branch) && firstLoad)
                {
                    cxToolbar.BranchesCombo.SelectedIndex = CxUtils.GetItemIndexInCombo(branch, cxToolbar.BranchesCombo, Enums.ComboboxType.BRANCHES);
                    firstLoad = false;
                }
            }
        }

        /// <summary>
        /// On change event for Branches combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnChangeBranch(object sender, SelectionChangedEventArgs e)
        {
            ComboBox branchesCombo = sender as ComboBox;
            if (branchesCombo == null || branchesCombo.SelectedItem == null || branchesCombo.SelectedIndex == -1) return;

            cxToolbar.EnableCombos(false);
            cxToolbar.ScansCombo.Text = string.IsNullOrEmpty(CxToolbar.currentScanId) ? CxConstants.TOOLBAR_LOADING_SCANS : CxToolbar.currentScanId;
            cxToolbar.ResultsTreePanel.ClearAllPanels();

            string selectedBranch = (branchesCombo.SelectedItem as ComboBoxItem).Content as string;
            string projectId = ((cxToolbar.ProjectsCombo.SelectedItem as ComboBoxItem).Tag as Project).Id;

            SettingsUtils.StoreToolbarValue(cxToolbar.Package, SettingsUtils.toolbarCollection, "branch", selectedBranch);

            _ = scansCombobox.LoadScansAsync(projectId, selectedBranch);
        }
    }
}