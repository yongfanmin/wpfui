// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using Wpf.Ui.Gallery.Services.Database;

namespace Wpf.Ui.Gallery.ViewModels.Pages;

public partial class CreatePrintTaskViewModel : ObservableObject
{
    private readonly IDatabaseService _databaseService;

    [ObservableProperty]
    private ObservableCollection<string> _produceBatchNumbers = new();

    [ObservableProperty]
    private string _destinationFolder = string.Empty;

    [ObservableProperty]
    private double _copyProgress = 0;

    [ObservableProperty]
    private bool _isExecuting = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartBatchPrintCommand))]
    private bool _isPrintButtonEnabled = false;

    public CreatePrintTaskViewModel(IEnumerable<string> produceBatchNumbers, IDatabaseService databaseService)
    {
        ProduceBatchNumbers = new ObservableCollection<string>(produceBatchNumbers);
        _databaseService = databaseService;
    }

    [RelayCommand]
    private void OnSelectFolder()
    {
        var openFolderDialog = new Microsoft.Win32.OpenFolderDialog();
        if (openFolderDialog.ShowDialog() == true)
        {
            DestinationFolder = openFolderDialog.FolderName;
        }
    }

    [RelayCommand]
    private async Task OnExecute()
    {
        if (string.IsNullOrEmpty(DestinationFolder) || !Directory.Exists(DestinationFolder))
        {
            // Ideally, show a message to the user
            return;
        }

        IsExecuting = true;
        CopyProgress = 0;

        var allFilesToCopy = new List<string>();
        foreach (var batchNum in ProduceBatchNumbers)
        {
            /*var items = _databaseService.GetProduceItemList(batchNum);
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.SaveLocalInfo))
                {
                    // Assuming SaveLocalInfo stores the full path to the file
                    allFilesToCopy.Add(item.SaveLocalInfo);
                }
            }*/
        }

        for (int i = 0; i < allFilesToCopy.Count; i++)
        {
            var sourcePath = allFilesToCopy[i];
            var destinationPath = Path.Combine(DestinationFolder, Path.GetFileName(sourcePath));
            await Task.Run(() => File.Copy(sourcePath, destinationPath, true));
            CopyProgress = (double)(i + 1) / allFilesToCopy.Count * 100;
        }

        IsExecuting = false;
        IsPrintButtonEnabled = true;
    }

    [RelayCommand(CanExecute = nameof(CanStartBatchPrint))]
    private void OnStartBatchPrint()
    {
        // Logic to start batch printing will go here.
    }

    private bool CanStartBatchPrint()
    {
        return IsPrintButtonEnabled;
    }
}
