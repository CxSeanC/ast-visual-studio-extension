﻿using Microsoft;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    internal class InfobarService : IVsInfoBarUIEvents
    {
        private readonly IServiceProvider serviceProvider;
        private uint cookie;
        private InfobarService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }
        public static InfobarService Instance { get; private set; }
        public static InfobarService Initialize(IServiceProvider serviceProvider)
        {
            if(Instance == null)
            {
                Instance = new InfobarService(serviceProvider);
            }
            
            return Instance;
        }

        public async Task ShowInfoBarWithLinkAsync(string message, ImageMoniker messageSeverity, string linkDisplayName, string linkId)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            InfoBarTextSpan text = new InfoBarTextSpan(message);
            InfoBarHyperlink hyperLink = new InfoBarHyperlink(linkDisplayName, linkId);
            InfoBarTextSpan[] spans = new InfoBarTextSpan[] { text };
            InfoBarActionItem[] actions = new InfoBarActionItem[] { hyperLink };
            InfoBarModel infoBarModel = new InfoBarModel(spans, actions, messageSeverity, isCloseButtonVisible: true);

            _ = ShowInfoBar(infoBarModel);
        }

        public async Task ShowInfoBarAsync(string message, ImageMoniker messageSeverity)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            InfoBarTextSpan text = new InfoBarTextSpan(message);
            InfoBarTextSpan[] spans = new InfoBarTextSpan[] { text };
            InfoBarModel infoBarModel = new InfoBarModel(spans, messageSeverity, isCloseButtonVisible: true);
            
           _ = ShowInfoBar(infoBarModel);
        }

        private async Task ShowInfoBar(InfoBarModel infoBarModel)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var factory = serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;

            Assumes.Present(factory);

            IVsInfoBarUIElement element = factory.CreateInfoBar(infoBarModel);

            element.Advise(this, out cookie);

            if (serviceProvider.GetService(typeof(SVsShell)) is IVsShell shell)
            {
                shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);

                var host = (IVsInfoBarHost)obj;
                if (host == null)
                {
                    return;
                }

                host.AddInfoBar(element);

                await Task.Delay(10000);

                host.RemoveInfoBar(element);
            }
        }

        public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string actionId = (string) actionItem.ActionContext;

            if (string.Equals(actionId, CxConstants.CODEBASHING_OPEN_HTTP_LINK_ID, StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Process.Start(actionItem.Text);
            }
        }

        public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            infoBarUIElement.Unadvise(cookie);
        }
    }
}
