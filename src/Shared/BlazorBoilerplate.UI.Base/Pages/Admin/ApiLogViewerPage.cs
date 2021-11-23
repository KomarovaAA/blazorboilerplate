using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorBoilerplate.Shared.Interfaces;
using BlazorBoilerplate.Shared.Localizer;
using Karambolo.Common.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Hosting;

namespace BlazorBoilerplate.UI.Base.Pages.Admin;

public class ApiLogViewerPage : ComponentBase
{
    protected List<ApiLogItem> ApiLogItems;
    [Inject] private IViewNotifier viewNotifier { get; set; }
    [Inject] private IApiClient apiClient { get; set; }
    [Inject] protected IStringLocalizer<Global> L { get; set; }
    protected int pageSize { get; set; } = 10;
    protected int pageIndex { get; set; }
    protected int totalItemsCount { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    protected async Task OnPage(int index, int size)
    {
        pageSize = size;
        pageIndex = index;

        await LoadData();
    }

    protected async Task LoadData()
    {
        try
        {
            var result = await apiClient.GetApiLogs(null, pageSize, pageIndex * pageSize);

            apiLogItems = new List<ApiLogItem>(result);
            totalItemsCount = (int)result.InlineCount.Value;
            viewNotifier.Show(L["One item found", Plural.From("{0} items found", totalItemsCount)],
                ViewNotifierType.Success, L["Operation Successful"]);
        }
        catch (Exception ex)
        {
            viewNotifier.Show(ex.GetBaseException().Message, ViewNotifierType.Error, L["Operation Failed"]);
        }

        await InvokeAsync(StateHasChanged);
    }
}