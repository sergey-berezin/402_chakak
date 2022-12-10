using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ModelsComponents;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Task3.Commands.Base;
using Task3.Models;
using System.Security.Cryptography;
using System.Linq;
using System;
using System.Reflection.Metadata;
using static System.Reflection.Metadata.BlobBuilder;
using System.Collections.ObjectModel;
using SixLabors.ImageSharp.Memory;
using System.Drawing.Imaging;
using System.Drawing;
using System.Net.Http;

namespace Task3.ViewModels
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
            //IsComparison = false;
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
        //-------------------------------------------------------------------

        //-------------------------------------------------------------------


        public MainWindowViewModel()
        {
            embeder = new ArcFaceComponent();

            StartComparisonCommand = new Command(StartComparisonCommandExecute, StartComparisonCommandCanExecute);
            CancelComparisonCommand = new Command(CancelComparisonCommandExecute, CancelComparisonCommandCanExecute);
            DeleteComparisonCommand = new Command(DeleteComparisonCommandExecute, DeleteComparisonCommandCanExecute);
            LoadComparisonCommand = new Command(LoadComparisonCommandExecute, LoadComparisonCommandCanExecute);
        }

        //-------------------------------------------------------------------
        public static string GetHash(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(data).Select(x => x.ToString("X2")));
            }
        }
        private async Task LoadComparison()
        {
            using (DataContext db = new DataContext())
            {
                if (db.Images.Any())
                {
                    var imagePaths = new List<string>();

                    foreach (var item in db.Images)
                    {
                        imagePaths.Add(item.Name);
                    }
                    ComparisonResults = new ComparisonResult[imagePaths.Count, imagePaths.Count];

                    foreach (var item in db.Embeddings)
                    {
                        ComparisonResults[item.PairImage1, item.PairImage2].distance = item.distance;
                        ComparisonResults[item.PairImage1, item.PairImage2].similarity = item.similarity;
                    }

                    ImagePaths = imagePaths.ToArray();
                }
                else
                {
                    MessageBox.Show("База данных пуста");
                }

            }
        }
        private async Task StartComparison()
        {
            var folderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            ProgressValue = 0;

            var imagePaths = new List<string>();
            var embeddings = new List<float[]>();
            if (folderDialog.ShowDialog() ?? false)
            {
                var folderPath = folderDialog.SelectedPaths[0];

                foreach (var imageFileInfo in new DirectoryInfo(folderPath).GetFiles())
                {
                    imagePaths.Add(imageFileInfo.FullName);
                }

                CountImages = imagePaths.Count;
                List<byte[]> data_byte = new List<byte[]>();
                foreach (var imageFile in imagePaths)
                {
                    byte[] img_byte = File.ReadAllBytes(imageFile);
                    string hashcode = GetHash(img_byte);
                    using (DataContext db = new DataContext())
                    {
                        var q1 = db.Images.Where(h => h.Hashcode == hashcode);
                        if (q1.FirstOrDefault() != null)
                        {
                            foreach (var item in q1)
                            {
                                var q2 = db.bytes.Where(b => b.ImageId == item.ImageId).FirstOrDefault();
                                if (q2 != null && q2.BLOB != img_byte)
                                {

                                    db.Images.Add(new Task3.Models.Image { Hashcode = hashcode, Name = imageFile });
                                    db.SaveChanges();
                                    var query = db.Images.OrderBy(d => d.ImageId).Last();
                                    db.bytes.Add(new byteImage { BLOB = img_byte, ImageId = query.ImageId });
                                    db.SaveChanges();
                                    embeddings.Add(await embeder.GetEmbeddingAsync(img_byte, cts.Token));
                                    ProgressValue += 1;

                                }
                            }

                        }
                        else
                        {
                            db.Images.Add(new Task3.Models.Image { Hashcode = hashcode, Name = imageFile });
                            db.SaveChanges();
                            var query = db.Images.OrderBy(d => d.ImageId).Last();
                            db.bytes.Add(new byteImage { BLOB = img_byte, ImageId = query.ImageId });
                            db.SaveChanges();
                            embeddings.Add(await embeder.GetEmbeddingAsync(img_byte, cts.Token));
                            ProgressValue += 1;
                        }
                    }
                }
                ImagePaths = imagePaths.ToArray();
                ComparisonResults = ComparisonResultCreator.Create(embeddings);
                using (DataContext db = new DataContext())
                {
                    int i = 0;
                    foreach (var item in db.Images)
                    {
                        for (int j = 0; j < ComparisonResults.GetLength(0); j++)
                        {
                            db.Embeddings.Add(new Embedding { ImageId = item.ImageId, distance = ComparisonResults[i, j].distance, similarity = ComparisonResults[i, j].similarity, PairImage1 = i, PairImage2 = j });
                        }
                        i++;
                    }
                    db.SaveChanges();
                }
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
        private void DeleteComparison()
        {

            using (DataContext db = new DataContext())
            {
                if (!db.Images.Any())
                {
                    MessageBox.Show("В базе данных нет изображений");
                }
                else
                {
                    if (ImagePaths != null)
                        ImagePaths = null;
                    if (ComparisonResults != null)
                        ComparisonResults = null;

                    if (db.Images.Any())
                    {
                        foreach (var img in db.Images)
                        {
                            db.Images.Remove(img);
                        }
                    }
                    db.SaveChanges();

                    if (db.bytes.Any())
                    {
                        foreach (var item in db.bytes)
                        {
                            db.bytes.Remove(item);
                        }
                    }
                    db.SaveChanges();

                    if (db.Embeddings.Any())
                    {
                        foreach (var embe in db.Embeddings)
                        {
                            db.Embeddings.Remove(embe);
                        }
                    }
                    db.SaveChanges();
                    MessageBox.Show("Данные были удалены");

                }
            }

        }
    }
}
