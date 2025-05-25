using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Views {
    public class BasePage : ContentPage {
        protected override async void OnAppearing() {
            base.OnAppearing();

            // Limpiar la pila de navegación si no estamos en la página raíz
            if (Navigation.NavigationStack.Count > 1) {
                for (int i = Navigation.NavigationStack.Count - 1; i > 0; i--) {
                    var page = Navigation.NavigationStack[i];
                    if (page != this) {
                        Navigation.RemovePage(page);
                    }
                }
            }

            await LoadDataAsync();
        }

        protected virtual Task LoadDataAsync() => Task.CompletedTask;
    }
}
