﻿@page "/detailsListPageBasic"
@using BlazorFluentUI.Lists
<header class="root">
    <h1 class="title">DetailsList Basic</h1>
</header>
<div class="section" style="transition-delay: 0s;">
    <div id="overview" tabindex="-1">
        <h2 class="subHeading hiddenContent">Overview</h2>
    </div>
    <div class="content">
        <div class="ms-Markdown">
            <p>
                A details list (<code>DetailsList</code>) is a robust way to display an information-rich collection of items, and allow people to sort, group, and filter the content. Use a details list when information density is critical.
            </p>
        </div>
    </div>
</div>
<div class="section" style="transition-delay: 0s;">
    <div id="overview" tabindex="-1">
        <h2 class="subHeading">Usage</h2>
    </div>
    <div>
        <div class="subSection">
            <Demo Header="Basic Details List" Key="0" MetadataPath="DetailsListPageBasic">
                <div data-is-scrollable="true" style="height:400px;overflow-y:auto;">
                    <DetailsList ItemsSource="InputList"
                                 Columns="Columns"
                                 GetKey=@(item=>item.Key)
                                 LayoutMode="DetailsListLayoutMode.Justified"
                                 TItem="DataItem"
                                 OnItemInvoked="OnClick"
                                 Selection="selection"
                                 SelectionMode="SelectionMode.Multiple">
                    </DetailsList>
                </div>
            </Demo>
        </div>
    </div>
</div>
@code {

    System.Collections.Generic.List<DataItem> InputList = new();

    Selection<DataItem> selection = new Selection<DataItem>();

    public System.Collections.Generic.List<DetailsRowColumn<DataItem>> Columns = new();

    protected override void OnInitialized()
    {
        selection.GetKey = (item => item.Key);
        Columns.Add(new DetailsRowColumn<DataItem>("Key", x => x.KeyNumber) { MaxWidth = 70, Index = 0 });
        Columns.Add(new DetailsRowColumn<DataItem>("Name", x => x.DisplayName) { Index = 1, MaxWidth = 150, OnColumnClick = this.OnColumnClick, IsResizable = true });
        Columns.Add(new DetailsRowColumn<DataItem>("Description", x => x.Description) { Index = 2 });

        int count = 0;
        for (var i = 0; i < 1000; i++)
        {
            count++;

            InputList.Add(new DataItem(count));
        }

        base.OnInitialized();
    }

    private void OnColumnClick(DetailsRowColumn<DataItem> column)
    {
        // since we're creating a new list, we need to make a copy of what was previously selected
        var selected = selection.GetSelection();

        //create new sorted list
        InputList = new System.Collections.Generic.List<DataItem>(column.IsSorted ? InputList.OrderBy(x => x.DisplayName) : InputList.OrderByDescending(x => x.DisplayName));

        //clear old selection and create new selection
        //selection.SetKeySelected(selected, true);

        column.IsSorted = !column.IsSorted;
        StateHasChanged();
    }

    private void OnClick(DataItem item)
    {
        Console.WriteLine("Clicked!");
    }
}