using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SCPP_WinUI_CS.Models
{
    // This class implements INotifyPropertyChanged
    // to support one-way and two-way bindings
    // (such that the UI element updates when the source
    // has been changed dynamically)
    // Usar struct con la interfaz no funciono. Pero el mismo
    // codigo con una clase si, asi que cambiamos a clase y copiamos codigo
    // de ejemplo de Microsoft
    // https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/how-to-implement-property-change-notification?view=netframeworkdesktop-4.8
    public class GetDocsForm : INotifyPropertyChanged
    {
        private string _searchPhrase;
        private int _fk_tipoDoc;
        private int _fk_categoria;
        private DateOnly _fechaInicio;
        private DateOnly _fechaTermino;
        // Declare the event
        public event PropertyChangedEventHandler PropertyChanged;

        public GetDocsForm()
        {
        }
        public int fk_tipoDoc
        {
            get { return _fk_tipoDoc; }
            set
            {
                // Al parecer el Combobox se resetea cada vez que se setea y causa loop
                if (_fk_tipoDoc == value)
                {
                    return;
                }
                _fk_tipoDoc = value;
                OnPropertyChanged();
            }
        }
        public int fk_categoria
        {
            get { return _fk_categoria; }
            set
            {
                // Al parecer el Combobox se resetea cada vez que se setea y causa loop
                if (_fk_categoria == value)
                {
                    return;
                }
                _fk_categoria = value;
                OnPropertyChanged();
            }
        }
        public string searchPhrase
        {
            get { return _searchPhrase; }
            set
            {
                _searchPhrase = value;
                OnPropertyChanged();
            }
        }
        public DateOnly fechaInicio
        {
            get { return _fechaInicio; }
            set
            {
                _fechaInicio = value;
                //OnPropertyChanged();
                // Las fechas se disparan 2 veces, una en el calendar y otra cuando el calendar
                // actualiza la fecha al parecer. eliminamos este trigger asi se ejecuta 1 vez la funcion
                // no 2 veces
            }
        }
        public DateOnly fechaTermino
        {
            get { return _fechaTermino; }
            set
            {
                _fechaTermino = value;
                //OnPropertyChanged();
                // Las fechas se disparan 2 veces, una en el calendar y otra cuando el calendar
                // actualiza la fecha al parecer. eliminamos este trigger asi se ejecuta 1 vez la funcion
                // no 2 veces
            }
        }
        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
