using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ModelsComponents;
using Task2.Commands.Base;
using Task2.Models;



namespace Task2.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase
    {
        private readonly ArcFaceComponent embeder;
        private CancellationTokenSource cts;

        //-------------------------------------------------------------------
        private bool _isComparison = true;
        public bool IsComparison
        {
            get => _isComparison;
            set => Set(ref _isComparison, value);
        }

        private int _countImages = 1;
        public int CountImages
        {
            get => _countImages;
            set => Set(ref _countImages, value);
        }

        private int _progressValue = 0;
        public int ProgressValue
        {
            get => _progressValue;
            set => Set(ref _progressValue, value);
        }

        //-------------------------------------------------------------------

        private string[] _imageName;
        public string[] ImageName
        {
            get => _imageName;
            set => Set(ref _imageName, value);
        }

        private ComparisonResult[,] _comparisonResults = null;
        public ComparisonResult[,] ComparisonResults
        {
            get => _comparisonResults;
            set => Set(ref _comparisonResults, value);
        }


        //-------------------------------------------------------------------
        public Command StartComparisonCommand { get; }
        private async void StartComparisonCommandExecute(object _)
        {
            cts = new CancellationTokenSource();
            IsComparison = false;
            await StartComparison();
            IsComparison = true;
            cts.Dispose();
        }
        private bool StartComparisonCommandCanExecute(object _)
        {
            return IsComparison;
        }

        public Command CancelComparisonCommand { get; }
        private void CancelComparisonCommandExecute(object _)
        {
            CancelComparison();
            IsComparison = true;
        }
        private bool CancelComparisonCommandCanExecute(object _)
        {
            return !IsComparison;
        }

        //-------------------------------------------------------------------
        public MainWindowViewModel()
        {
            embeder = new ArcFaceComponent();

            StartComparisonCommand = new Command(StartComparisonCommandExecute, StartComparisonCommandCanExecute);
            CancelComparisonCommand = new Command(CancelComparisonCommandExecute, CancelComparisonCommandCanExecute);
        }

        //-------------------------------------------------------------------
        private async Task StartComparison()
        {
            var folderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            ProgressValue = 0;

            var imageNames = new List<string>();
            var imagePaths = new List<string>();
            var embeddings = new List<float[]>();


            if (folderDialog.ShowDialog() ?? false)
            {
                var folderPath = folderDialog.SelectedPaths[0];

                foreach (var imageFileInfo in new DirectoryInfo(folderPath).GetFiles())
                {
                    imageNames.Add(imageFileInfo.Name);
                    imagePaths.Add(imageFileInfo.FullName);
                }

                CountImages = imageNames.Count;

                await foreach (var item in embeder.GetEmbeddingAsync(imagePaths).WithCancellation(cts.Token))
                {
                    embeddings.Add(item);
                    ProgressValue += 1;
                }

                ImageName = imageNames.ToArray();
                ComparisonResults = ComparisonResultCreator.Create(embeddings);

                if (_progressValue == _countImages)
                {
                    MessageBox.Show("Распознавание окончено.", "Внимание");
                }
            }
        }

        private void CancelComparison()
        {
            cts.Cancel();
            MessageBox.Show("Вы остановили процесс распознавания.", "Внимание");
        }
    }
}
