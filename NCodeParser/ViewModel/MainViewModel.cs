using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using NCodeParser.IO;
using NCodeParser.Model;
using NCodeParser.View;

namespace NCodeParser.ViewModel
{
	public class MainViewModel : ViewModelBase
	{
		public RelayCommand AddCommand1
		{
			get; private set;
		}

		public RelayCommand AddCommand2
		{
			get; private set;
		}

		public RelayCommand AddCommand3
		{
			get; private set;
		}

		public RelayCommand SelectAllCommand
		{
			get; private set;
		}

		public RelayCommand DownloadCommand
		{
			get; private set;
		}

		public RelayCommand ClosingCommand
		{
			get;
			private set;
		}

		public RelayCommand SettingCommand
		{
			get;
			private set;
		}

		public ObservableCollection<Novel> NovelList
		{
			get; private set;
		}

		public Novel SelectedNovel
		{
			get
			{
				return _SelectedNovel;
			}
			set
			{
				_SelectedNovel = value;
				RaisePropertyChanged();

				if (SelectedNovel != null)
				{
					SelectNovel(SelectedNovel);
				}
			}
		}

		public string Code1
		{
			get
			{
				return _Code1;
			}
			set
			{
				_Code1 = value;
				RaisePropertyChanged();
			}
		}

		public string Code2
		{
			get
			{
				return _Code2;
			}
			set
			{
				_Code2 = value;
				RaisePropertyChanged();
			}
		}

		public string Code3
		{
			get
			{
				return _Code3;
			}
			set
			{
				_Code3 = value;
				RaisePropertyChanged();
			}
		}

		public bool Downloading
		{
			get
			{
				return _Downloading;
			}
			set
			{
				_Downloading = value;
				RaisePropertyChanged();
				RaisePropertyChanged(nameof(Downloadable));
			}
		}

		public bool ShowProgress
		{
			get
			{
				if (SelectedNovel == null)
				{
					return false;
				}
				else
				{
					return SelectedNovel.ShowProgress;
				}
			}
		}

		public bool Downloadable
		{
			get
			{
				return !Downloading;
			}
		}

		private Novel _SelectedNovel;
		private string _Code1;
		private string _Code2;
		private string _Code3;
		private bool _Downloading;

		private NovelDownloader Downloader;
		private INIManager INIManager;

		public MainViewModel()
		{
			InitInstance();
			InitControls();
		}

		private void InitInstance()
		{
			AddCommand1 = new RelayCommand(OnAdd1, CanAdd1);
			AddCommand2 = new RelayCommand(OnAdd2, CanAdd2);
			AddCommand3 = new RelayCommand(OnAdd3, CanAdd3);
			SelectAllCommand = new RelayCommand(OnSelectAll, CanSelectAll);
			DownloadCommand = new RelayCommand(OnDownload, CanDownload);
			ClosingCommand = new RelayCommand(OnClosing);
			SettingCommand = new RelayCommand(OnSetting);

			Downloader = new NovelDownloader();
			Downloader.ProgressChanged += Downloader_ProgressChanged;

			INIManager = new INIManager();
		}

		private void InitControls()
		{
			NovelList = new ObservableCollection<Novel>();
			NovelList.AddAll(INIManager.GetNovels());

			if (NovelList.Count > 0)
			{
				SelectedNovel = NovelList[0];
			}

			CheckAllUpdate();
		}

		private async void SelectNovel(Novel novel)
		{
			if (novel == null)
			{
				return;
			}

			if (novel.Episodes.Count == 0)
			{
				var Episodes = await Task.Run(() =>
				{
					return Downloader.DownloadList(novel);
				});

				if (Episodes != null)
				{
					novel.Episodes.Clear();
					novel.Episodes.AddAll(Episodes);

					CommandManager.InvalidateRequerySuggested();
				}
			}

			if (novel.EpisodeStartIndex == -1 && novel.EpisodeEndIndex == -1)
			{
				novel.EpisodeStartIndex = 0;
				novel.EpisodeEndIndex = novel.Episodes.Count - 1;
			}

			if (novel.Episodes.Count > 0 && string.IsNullOrWhiteSpace(novel.Episodes[0].Text))
			{
				novel.ShowProgress = true;
				RaisePropertyChanged(nameof(ShowProgress));

				await Task.Run(() =>
				{
					Downloader.DownloadNovel(novel, 0, 0, false, true);
				});

				novel.RaisePropertyChanged(nameof(novel.DescWithPrologue));

				novel.ShowProgress = false;
				RaisePropertyChanged(nameof(ShowProgress));
			}
		}

		private void CheckAllUpdate()
		{
			for (int i = 0; i < NovelList.Count; i++)
			{
				CheckUpdate(NovelList[i]);
			}
		}

		private async void CheckUpdate(Novel novel)
		{
			if (novel == null)
			{
				return;
			}

			if (novel.Episodes.Count == 0)
			{
				var Episodes = await Task.Run(() =>
				{
					return Downloader.DownloadList(novel);
				});

				if (Episodes != null)
				{
					novel.Episodes.Clear();
					novel.Episodes.AddAll(Episodes);

					CommandManager.InvalidateRequerySuggested();
				}
			}

			novel.EpisodeStartIndex = 0;
			novel.EpisodeEndIndex = novel.Episodes.Count - 1;

			string filePath = AppDomain.CurrentDomain.BaseDirectory + novel.Name;

			if (novel.Episodes.Count == 0)
			{
				novel.UpdateCount = 0;
				return;
			}

			if (!Directory.Exists(filePath))
			{
				novel.UpdateCount = novel.Episodes.Count;
				return;
			}

			var MyDirectory = new DirectoryInfo(filePath);
			var Files = MyDirectory.GetFiles("*.txt");

			int LastNumber = novel.Episodes[novel.Episodes.Count - 1].Number;
			int Max = 0;

			for (int i = 0; i < Files.Length; i++)
			{
				var FileName = Path.GetFileNameWithoutExtension(Files[i].FullName);

				int Number;
				bool IsSuccess;

				if (FileName.Contains("~"))
				{
					var SplitStrings = FileName.Split('~');

					IsSuccess = int.TryParse(SplitStrings[SplitStrings.Length - 1], out Number);
					if (IsSuccess && LastNumber == Number)
					{
						Max = LastNumber;
						break;
					}

					if (IsSuccess && Max < Number)
					{
						Max = Number;
					}
				}

				IsSuccess = int.TryParse(FileName, out Number);
				if (!IsSuccess)
				{
					continue;
				}

				if (LastNumber == Number)
				{
					Max = LastNumber;
					break;
				}

				if (Max < Number)
				{
					Max = Number;
				}
			}

			novel.UpdateCount = LastNumber - Max;

			if (novel.UpdateCount > 0)
			{
				novel.EpisodeStartIndex = novel.EpisodeEndIndex - (novel.UpdateCount - 1);
			}
		}

		private void OnAdd1()
		{
			if (NovelList.Any(R => R.Code == Code1))
			{
				SelectedNovel = NovelList.FirstOrDefault(R => R.Code == Code1);
				Code1 = "";

				MessageBox.Show("이미 등록된 소설입니다.");

				return;
			}

			var Novel = new Novel
			{
				Type = NovelType.Normal,
				Code = Code1
			};

			NovelList.Add(Novel);

			Code1 = "";
		}

		private bool CanAdd1()
		{
			if (string.IsNullOrWhiteSpace(Code1))
			{
				return false;
			}

			return true;
		}

		private void OnAdd2()
		{
			if (NovelList.Any(R => R.Code == Code2))
			{
				SelectedNovel = NovelList.FirstOrDefault(R => R.Code == Code2);
				Code2 = "";

				MessageBox.Show("이미 등록된 소설입니다.");

				return;
			}

			var Novel = new Novel
			{
				Type = NovelType.R18,
				Code = Code2
			};

			NovelList.Add(Novel);

			Code2 = "";
		}

		private bool CanAdd2()
		{
			if (string.IsNullOrWhiteSpace(Code2))
			{
				return false;
			}

			return true;
		}

		private void OnAdd3()
		{
			if (NovelList.Any(R => R.Code == Code3))
			{
				SelectedNovel = NovelList.FirstOrDefault(R => R.Code == Code3);
				Code3 = "";

				MessageBox.Show("이미 등록된 소설입니다.");

				return;
			}

			var Novel = new Novel
			{
				Type = NovelType.Kakuyomu,
				Code = Code3
			};

			NovelList.Add(Novel);

			Code3 = "";
		}

		private bool CanAdd3()
		{
			if (string.IsNullOrWhiteSpace(Code3))
			{
				return false;
			}

			return true;
		}

		private void OnSelectAll()
		{
			SelectedNovel.EpisodeStartIndex = 0;
			SelectedNovel.EpisodeEndIndex = SelectedNovel.Episodes.Count - 1;
		}

		private bool CanSelectAll()
		{
			if (SelectedNovel == null)
			{
				return false;
			}

			if (SelectedNovel.Episodes.Count == 0)
			{
				return false;
			}

			if (!Downloadable)
			{
				return false;
			}

			return true;
		}

		private void OnDownload()
		{
			Downloading = true;

			SelectedNovel.ProgressMax = (SelectedNovel.EpisodeEndIndex - SelectedNovel.EpisodeStartIndex) + 1;

			Task.Run(() =>
			{
				Downloader.DownloadNovel(SelectedNovel, SelectedNovel.EpisodeStartIndex, SelectedNovel.EpisodeEndIndex, SelectedNovel.Merging);

				Downloading = false;

				CommandManager.InvalidateRequerySuggested();
			});
		}

		private bool CanDownload()
		{
			if (SelectedNovel == null)
			{
				return false;
			}

			if (SelectedNovel.Episodes.Count == 0)
			{
				return false;
			}

			if (!Downloadable)
			{
				return false;
			}

			return true;
		}

		private void OnClosing()
		{
			INIManager.SetNovels(NovelList);
		}

		private void OnSetting()
		{
			var window = new SettingWindow();
			window.ShowDialog();
		}

		private void Downloader_ProgressChanged(object sender, int Value)
		{
			var Novel = sender as Novel;
			Novel.ProgressValue = Value;
		}
	}
}
