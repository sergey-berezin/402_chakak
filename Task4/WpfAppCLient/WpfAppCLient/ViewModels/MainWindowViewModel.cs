using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System;
using System.Net.Http;
using WpfAppCLient.Commands.Base;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Polly;
using Polly.Retry;

namespace WpfAppCLient.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase
    {
        private CancellationTokenSource cts;

        //-------------------------------------------------------------------

        public string url = "https://localhost:7230/";
        public string imagesId
        {
            get;
            set;
        }
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

        private string[] _imagePaths;
        public string[] ImagePaths
        {
            get => _imagePaths;
            set => Set(ref _imagePaths, value);
        }

        private ComparisonResult[,] _comparisonResults = null;
        public ComparisonResult[,] ComparisonResults
        {
            get => _comparisonResults;
            set => Set(ref _comparisonResults, value);
        }


        //-------------------------------------------------------------------
        public Command LoadComparisonCommand { get; }
        private async void LoadComparisonCommandExecute(object _)
        {

            await LoadComparison();

        }
        private bool LoadComparisonCommandCanExecute(object _)
        {

            return IsComparison;
        }

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
        public Command DeleteComparisonCommand { get; }
        private void DeleteComparisonCommandExecute(object _)
        {
            DeleteComparison();
            IsComparison = true;
        }
        private bool DeleteComparisonCommandCanExecute(object _)
        {
            return IsComparison;
        }
        private AsyncRetryPolicy retryPolicy;
        private Random jitterer = new Random();

        public MainWindowViewModel()
        {
            retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(5,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))  // exponential back-off: 2, 4, 8 etc
                                  + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)));  // plus some jitter: up to 1 second
            StartComparisonCommand = new Command(StartComparisonCommandExecute, StartComparisonCommandCanExecute);
            CancelComparisonCommand = new Command(CancelComparisonCommandExecute, CancelComparisonCommandCanExecute);
            DeleteComparisonCommand = new Command(DeleteComparisonCommandExecute, DeleteComparisonCommandCanExecute);
            LoadComparisonCommand = new Command(LoadComparisonCommandExecute, LoadComparisonCommandCanExecute);
        }

        private async Task LoadComparison()
        {
            var imagePaths = new List<string>();
            try
            {
                await retryPolicy.ExecuteAsync(async () =>
                {

                    var httpclient = new HttpClient();
                    httpclient.BaseAddress = new Uri(url);
                    httpclient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage response = await httpclient.GetAsync("api/Images/");

                    List<byteImage> images = await response.Content.ReadFromJsonAsync<List<byteImage>>();
                    foreach (var item in images)
                    {
                        imagePaths.Add(item.Name);
                    }

                });

            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка доступа к серверу!");
            }
            finally
            {

            }

            try
            { 
                await retryPolicy.ExecuteAsync(async () =>
                {

                    var httpclient = new HttpClient();
                    httpclient.BaseAddress = new Uri(url);
                    httpclient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                        if (imagePaths.Count > 0)
                        {
                             HttpResponseMessage response = await httpclient.GetAsync("api/Images/Res/");
                            string results = await response.Content.ReadFromJsonAsync<string>();
                            string[] ss = results.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            ComparisonResults = new ComparisonResult[imagePaths.Count, imagePaths.Count];
                            ImagePaths = imagePaths.ToArray();
                            foreach (string s in ss)
                            {
                                string[] res = s.Split(' ');
                                ComparisonResults[int.Parse(res[0]), int.Parse(res[1])].distance = float.Parse(res[2]);
                                ComparisonResults[int.Parse(res[0]), int.Parse(res[1])].similarity = float.Parse(res[3]);
                            }
                        }
                        else
                        {
                            MessageBox.Show("База данных пуста");
                        }

                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка доступа к серверу!");
            }
            finally
            {

            }

        }
        private async Task StartComparison()
        {
            var folderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            ProgressValue = 0;

            var imagePaths = new List<string>();
            var embeddings = new List<float[]>();
            List<byteImage> images = new List<byteImage>();
            if (folderDialog.ShowDialog() ?? false)
            {
                var folderPath = folderDialog.SelectedPaths[0];

                foreach (var imageFileInfo in new DirectoryInfo(folderPath).GetFiles())
                {
                    imagePaths.Add(imageFileInfo.FullName);
                }
                ImagePaths = imagePaths.ToArray();
                CountImages = imagePaths.Count;
                List<byte[]> data_byte = new List<byte[]>();
                foreach (var imageFile in imagePaths)
                {
                    images.Add(new byteImage
                    {
                        BLOB = File.ReadAllBytes(imageFile),
                        Name = imageFile
                    });
                }
                try
                {
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        var httpclient = new HttpClient();
                        httpclient.BaseAddress = new Uri(url);
                        httpclient.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage response = await HttpClientJsonExtensions.PostAsJsonAsync(httpclient, "api/Images", images, cts.Token);

                        response.EnsureSuccessStatusCode();
                        List<int> resId = await response.Content.ReadFromJsonAsync<List<int>>();
                        ProgressValue += images.Count;
                        foreach (var item in resId)
                        {
                            imagesId += item.ToString() + "\n";
                        }
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка доступа к серверу!");
                }
                finally
                {
                    
                }


                try
                {
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        var httpclient = new HttpClient();
                        httpclient.BaseAddress = new Uri(url);
                        httpclient.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage response = await httpclient.GetAsync("api/Images/Res/");
                        string results = await response.Content.ReadFromJsonAsync<string>();
                        string[] ss = results.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        ComparisonResults = new ComparisonResult[imagePaths.Count, imagePaths.Count];

                        foreach (string s in ss)
                        {
                            string[] res = s.Split(' ');
                            ComparisonResults[int.Parse(res[0]), int.Parse(res[1])].distance = float.Parse(res[2]);
                            ComparisonResults[int.Parse(res[0]), int.Parse(res[1])].similarity = float.Parse(res[3]);
                        }
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка доступа к серверу!");
                }
                finally
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
        private async void DeleteComparison()
        {
            if (ImagePaths != null)
                ImagePaths = null;
            if (ComparisonResults != null)
                ComparisonResults = null;
            try
            {
                await retryPolicy.ExecuteAsync(async () =>
                {
                    var httpclient = new HttpClient();
                    httpclient.BaseAddress = new Uri(url);
                    httpclient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage response = await httpclient.DeleteAsync("/api/Images/");
                    response.EnsureSuccessStatusCode();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка доступа к серверу!");
            }
            finally
            {
                MessageBox.Show("Данные были удалены");
            }

        }
    }
}
