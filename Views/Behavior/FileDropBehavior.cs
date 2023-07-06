using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ShotgunMetagenome.Views.Behavior
{
    public class FileDropBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(FileDropBehavior), new PropertyMetadata(null));

        public ICommand Command
        {
            get
            {
                return (ICommand)GetValue(CommandProperty);
            }
            set
            {
                SetValue(CommandProperty, value);
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            base.AssociatedObject.AllowDrop = true;
            base.AssociatedObject.PreviewDragOver += OnPreviewDragOver;
            base.AssociatedObject.Drop += OnDrop;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            base.AssociatedObject.PreviewDragOver -= OnPreviewDragOver;
            base.AssociatedObject.Drop -= OnDrop;
        }

        protected void OnPreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, autoConvert: true))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else if (e.Data.GetDataPresent("UniformResourceLocator"))
            {
                e.Effects = DragDropEffects.Link;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private IEnumerable<Uri> ToUrlList(IDataObject data)
        {
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                return (data.GetData(DataFormats.FileDrop) as string[]).Select((string s) => new Uri(s));
            }

            Uri uri = new Uri(data.GetData(DataFormats.Text).ToString());
            return new Uri[1]
            {
                uri
            };
        }

        protected void OnDrop(object sender, DragEventArgs e)
        {
            if (Command != null && Command.CanExecute(e))
            {
                IEnumerable<Uri> enumerable = ToUrlList(e.Data);
                if (enumerable != null)
                {
                    Command.Execute(enumerable);
                }
            }
        }
    }
}
