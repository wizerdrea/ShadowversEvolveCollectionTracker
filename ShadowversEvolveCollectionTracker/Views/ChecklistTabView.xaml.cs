using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ShadowversEvolveCardTracker.Views
{
    public partial class ChecklistTabView : UserControl
    {
        private bool _isPanning;
        private Point _panStartPoint;
        private double _startTranslateX;
        private double _startTranslateY;

        public ChecklistTabView()
        {
            InitializeComponent();
        }
    }
}