using System;
using System.ComponentModel;

namespace OrderProcessingApp.Models
{
    public class Order : INotifyPropertyChanged
    {
        private string _status;
        public int Id { get; set; }
        public string OrderId { get; set; }
        public string MenuItem {  get; set; }
        public int Quantity { get; set; }
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
