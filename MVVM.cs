using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UPDialogTool
{
	/**Viewmodel only knows about the model, not the view.  Bridge between model and view.  Tells view to update when model changes*/
	public abstract class ObservableObject : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected void RaisePropertyChangedEvent(string propertyName)
		{
			var handler = PropertyChanged;
			if(handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	///<summary>
	/// ---ViewModel---
	/// </summary>
	/**Binds commands in the view*/
	public class DelegateCommand : ICommand
	{
		private readonly Action action;

		public DelegateCommand(Action action)
		{
			this.action = action;
		}

		public void Execute(object parameter)
		{
			action();
		}

		/**Allows buttons to give a warning message on failure rather than disabling*/
		public bool CanExecute(object parameter)
		{
			return true;
		}

		#pragma warning disable 67
			public event EventHandler CanExecuteChanged;
		#pragma warning restore 67

		public class Presenter : ObservableObject
		{

		}
		/// <summary>
		/// ---MODEL---
		/// </summary>
		public class TextConverter
		{
			public readonly Func<string, string> convertion;

			public TextConverter(Func<string, string> convertion)
			{
				this.convertion = convertion;
			}

			public string ConvertText(string inputText)
			{
				return convertion(inputText);
			}
		}

	}
}
