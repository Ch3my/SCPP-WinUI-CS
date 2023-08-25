using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SCPP_WinUI_CS.Dashboard;
using CommunityToolkit.Mvvm.ComponentModel;
using SCPP_WinUI_CS.Models;

namespace SCPP_WinUI_CS.PageModels
{
    public class AssetsPageViewModel : ObservableObject
    {
        public AssetsPageViewModel() { }
        public ObservableCollection<AssetRow> assetRows { get; set; } = new();
        public GetAssetsForm getAssetsForm { get; set; } = new();
        public Asset newAsset { get; set; } = new();
        public Asset editAsset { get; set; } = new();
        public struct GetAssetsForm
        {
            public string searchPhrase { get; set; }
        }

        public struct AssetRow
        {
            public int Id { get; set; }
            public string Fecha { get; set; }
            public string Descripcion { get; set; }
            public string AssetData { get; set; }
            // TODO add Categoria
        }
    }
}
