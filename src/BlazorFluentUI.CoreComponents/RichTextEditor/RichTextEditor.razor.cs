﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace BlazorFluentUI
{
    public partial class RichTextEditor : FluentUIComponentBase
    {
        
        [Inject] private IJSRuntime jsRuntime { get; set; }

        [Parameter] public bool Disabled { get; set; }

        [Parameter] public bool ReadOnly { get; set; }

        [Parameter] public string RichText { get; set; }

        [Parameter] public EventCallback<string> RichTextChanged { get; set; }
               
        private System.Collections.Generic.List<CommandBarItem> items;
        private bool hasFocus = false;

        private bool isImageDialogOpen = false;

        private string imageUrl = "";
        private string imageHeight = "";
        private string imageWidth = "";
        private string imageAlt = "";

        private string internalRichText = "";  //keeps track of changes so we know when we have to update the quilljs contents.

        private RelayCommand buttonCommand;
        private int quillId;
        private bool _renderedOnce;
        private Timer _debounceTextTimer;
        private string _waitingText;
        private Timer _debounceSelectionTimer;
        private FormattingState _waitingFormattingState;
        private bool _readonlySet;

        public RichTextEditor()
        {
            buttonCommand = new RelayCommand(async (p) =>
            {
                CommandBarItem? item = items.FirstOrDefault(x => x.Key == p.ToString());
                if (item != null)
                {
                    if (item.CanCheck)
                    {
                        if (!item.Checked)
                            await jsRuntime.InvokeVoidAsync("BlazorFluentUIRichTextEditor.setFormat", quillId, p.ToString().ToLowerInvariant());
                        else
                            await jsRuntime.InvokeVoidAsync("BlazorFluentUIRichTextEditor.setFormat", quillId, p.ToString().ToLowerInvariant(), false);
                        item.Checked = !item.Checked;
                    }
                    else
                    {
                        switch (item.Key)
                        {
                            case "Image":
                                isImageDialogOpen = true;
                                //await jsRuntime.InvokeVoidAsync("window.BlazorFluentUIRichTextEditor.setFormat", quillId, "image", "";
                                break;
                        }

                    }
                }
                StateHasChanged();
            });

            _debounceTextTimer = new System.Timers.Timer
            {
                Interval = 150,
                AutoReset = false
            };
            _debounceTextTimer.Elapsed += async (s, e) => 
            {
                await InvokeAsync(async () =>
                {
                    await RichTextChanged.InvokeAsync(_waitingText);
                });
            };

            _debounceSelectionTimer = new System.Timers.Timer
            {
                Interval = 150,
                AutoReset = false
            };
            _debounceSelectionTimer.Elapsed += async (s, e) =>
            {
                await InvokeAsync(() =>
                {
                    if (_waitingFormattingState != null)
                    {
                        bool stateNeedsChanging = false;
                        PropertyInfo[]? props = _waitingFormattingState.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (PropertyInfo? prop in props)
                        {
                            CommandBarItem? commandButton = items.FirstOrDefault(x => x.Key == prop.Name);
                            if (commandButton != null && commandButton.Checked != (bool)prop.GetValue(_waitingFormattingState))
                            {
                                commandButton.Checked = !commandButton.Checked;
                                stateNeedsChanging = true;
                            }
                        }
                        if (stateNeedsChanging)
                            StateHasChanged();
                    }
                });
            };

            items = new System.Collections.Generic.List<CommandBarItem> {
                new CommandBarItem() { Text= "Bold", CanCheck=true, IconOnly=true, IconName="Bold", Key="Bold", Command=buttonCommand, CommandParameter="Bold"},
                new CommandBarItem() { Text= "Italic", CanCheck=true, IconOnly=true, IconName="Italic", Key="Italic", Command=buttonCommand, CommandParameter="Italic"},
                new CommandBarItem() { Text= "Underline", CanCheck=true, IconOnly=true, IconName="Underline", Key="Underline", Command=buttonCommand, CommandParameter="Underline"},
                new CommandBarItem() { Text= "Superscript", CanCheck=true, IconOnly=true, IconName="Superscript", Key="Superscript", Command=buttonCommand, CommandParameter="Superscript"},
                new CommandBarItem() { Text= "Subscript", CanCheck=true, IconOnly=true, IconName="Subscript", Key="Subscript", Command=buttonCommand, CommandParameter="Subscript"},

                new CommandBarItem() { Text= "Insert Image", CanCheck=false, IconOnly=true, IconName="ImagePixel", Key="Image", Command=buttonCommand, CommandParameter="Image"}
            };
        }

        [JSInvokable]
        public Task TextChangedAsync(TextChangedArgs args)
        {
            //if (args.Source != ChangeSource.User)
            {
                if (_debounceTextTimer.Enabled)
                    _debounceTextTimer.Stop();

                internalRichText = args.Html;

                if (args.Html != RichText)
                {
                    _waitingText = args.Html;
                    _debounceTextTimer.Start();
                }
            }
            return Task.CompletedTask;
        }

        [JSInvokable]
        public Task SelectionChangedAsync(FormattingState formattingState)
        {
            if (_debounceSelectionTimer.Enabled)
                _debounceSelectionTimer.Stop();

            _waitingFormattingState = formattingState;
            _debounceSelectionTimer.Start();

            return Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (_renderedOnce)
            {               
                if (RichText != internalRichText)
                    await jsRuntime.InvokeVoidAsync("BlazorFluentUIRichTextEditor.setHtmlContent", quillId, RichText);
                if (ReadOnly && !_readonlySet)
                {
                    await jsRuntime.InvokeVoidAsync("BlazorFluentUIRichTextEditor.setReadonly", quillId, true);
                    _readonlySet = true;
                }
                else if (!ReadOnly && _readonlySet)
                {
                    await jsRuntime.InvokeVoidAsync("BlazorFluentUIRichTextEditor.setReadonly", quillId, false);
                    _readonlySet = false;
                }
            }
            await base.OnParametersSetAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                quillId = await jsRuntime.InvokeAsync<int>("BlazorFluentUIRichTextEditor.register", RootElementReference, DotNetObjectReference.Create(this));
                await jsRuntime.InvokeVoidAsync("BlazorFluentUIRichTextEditor.setHtmlContent", quillId, RichText);
                if (ReadOnly)
                {
                    await jsRuntime.InvokeVoidAsync("window.BlazorFluentUIRichTextEditor.setReadonly", quillId, true);
                    _readonlySet = true;
                }
                _renderedOnce = true;

            }
            await base.OnAfterRenderAsync(firstRender);
        }

        
        private async Task UpdateFormatStateAsync()
        {
            FormattingState? formatState = await jsRuntime.InvokeAsync<FormattingState>("BlazorFluentUIRichTextEditor.getFormat", quillId);
            if (formatState != null)
            {
                bool stateNeedsChanging = false;
                PropertyInfo[]? props = formatState.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (PropertyInfo? prop in props)
                {
                    CommandBarItem? commandButton = items.FirstOrDefault(x => x.Key == prop.Name);
                    if (commandButton != null && commandButton.Checked != (bool)prop.GetValue(formatState))
                    {
                        commandButton.Checked = !commandButton.Checked;
                        stateNeedsChanging = true;
                    }
                }
                if (stateNeedsChanging)
                    StateHasChanged();
            }

            //return Task.CompletedTask;
        }

        private async Task InterceptKeyPressAsync(KeyboardEventArgs keyboardEventArgs)
        {
            if (keyboardEventArgs.CtrlKey && keyboardEventArgs.Key == "+")
            {
                CommandBarItem? item = items.FirstOrDefault(x => x.Key == "Superscript");
                if (item != null)
                {
                    if (!item.Checked)
                        await jsRuntime.InvokeVoidAsync("BlazorFluentUIRichTextEditor.setFormat", quillId, "superscript");
                    else
                        await jsRuntime.InvokeVoidAsync("BlazorFluentUIRichTextEditor.setFormat", quillId, "superscript", false);


                    item.Checked = !item.Checked;
                }
            }
            else if (keyboardEventArgs.CtrlKey && keyboardEventArgs.Key == "=")
            {
                CommandBarItem? item = items.FirstOrDefault(x => x.Key == "Subscript");
                if (item != null)
                {
                    if (!item.Checked)
                        await jsRuntime.InvokeVoidAsync("BlazorFluentUIRichTextEditor.setFormat", quillId, "subscript");
                    else
                        await jsRuntime.InvokeVoidAsync("BlazorFluentUIRichTextEditor.setFormat", quillId, "subscript", false);


                    item.Checked = !item.Checked;
                }
            }
            //await UpdateFormatStateAsync();
        }

        private async Task OnFocusAsync()
        {
            await jsRuntime.InvokeVoidAsync("BlazorFluentUIRichTextEditor.preventZoomEnable", true);
        }

        private async Task OnBlurAsync()
        {
            await jsRuntime.InvokeVoidAsync("BlazorFluentUIRichTextEditor.preventZoomEnable", false);
        }

        private async Task InsertImageAsync()
        {
            await jsRuntime.InvokeVoidAsync(
                "BlazorFluentUIRichTextEditor.insertImage", 
                quillId, 
                imageUrl, 
                imageAlt, 
                string.IsNullOrWhiteSpace(imageWidth) ? null : imageWidth,
                string.IsNullOrWhiteSpace(imageHeight) ? null : imageHeight);
            imageUrl = "";
            imageAlt = "";
            imageWidth = "";
            imageHeight = "";
            isImageDialogOpen = false;
        }
    }
}
