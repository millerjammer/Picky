using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Picky
{
    /// <summary>
    /// Interaction logic for BoardView.xaml
    /// </summary>
    /// 
    public partial class BoardView : UserControl
    {
        private readonly BoardViewModel bvm;

        public BoardView()
        {
            InitializeComponent();
            bvm = new BoardViewModel();
            this.DataContext = bvm;
   
        }
    }
}
