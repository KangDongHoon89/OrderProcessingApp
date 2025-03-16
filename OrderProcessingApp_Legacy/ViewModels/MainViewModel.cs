using OrderProcessingApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace OrderProcessingApp.ViewModels
{
    public class MainViewModel
    {
        public ObservableCollection<Order> Orders { get; private set; }
        public RelayCommand AcceptOrderCommand { get; private set; }

        private readonly OrderSocketClient _socketClient;
        private readonly HttpClient _httpClient;

        public MainViewModel()
        {
            Orders = new ObservableCollection<Order>();
            _socketClient = new OrderSocketClient();
            _httpClient = new HttpClient();

            // RelayCommand로 버튼 명령어 설정
            AcceptOrderCommand = new RelayCommand(AcceptOrder, CanAcceptOrder);
            
            LoadPendingOrders();    // API에서 초기 주문 목록 불러오기
            InitializeAsync();      // 웹소켓 연결 및 이벤트 바인딩
        }

        private async void InitializeAsync()
        {
            await _socketClient.InitializeAsync();
            _socketClient.NewOrderReceived += OnNewOrderReceived;       // 주문 수신 이벤트 연결
            _socketClient.OrderStatusUpdated += OnOrderStatusUpdated;   // 주문 상태 업데이트 연결
        }

        private async void LoadPendingOrders()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:5000/api/orders/pending");
                if (response.IsSuccessStatusCode)
                {
                    var orders = await response.Content.ReadAsAsync<List<Order>>();
                    foreach (var order in orders)
                    {
                        Orders.Add(order);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"주문 목록을 불러오는 데 실패했습니다.\n오류: {ex.Message}", 
                                "오류", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error);
            }
        }

        // 주문이 도착하면 UI 목록에 추가
        private void OnNewOrderReceived(Order newOrder)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Orders.Add(newOrder);
            }));
        }

        private void OnOrderStatusUpdated(int id, string newStatus)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var order = Orders.FirstOrDefault(o => o.Id == id);
                if (order != null)
                {
                    // UI에 자동 반영됨
                    if (newStatus == "완료")
                    {
                        Orders.Remove(order);
                    }
                    else
                    {
                        order.Status = newStatus;
                    }
                }
            }));
        }

        public async Task UpdateOrderStatus(int id, string status)
        {
            await _socketClient.SendOrderStatusUpdate(id, status);
        }

        // 주문을 처리하는 메서드
        private async void AcceptOrder(object parameter)
        {
            if (parameter is Order selectedOrder)
            {
                string status = string.Empty;
                if (selectedOrder.Status == "대기 중")
                {
                    status = "준비 중";
                }
                else if (selectedOrder.Status == "준비 중")
                {
                    status = "완료";
                }

                await UpdateOrderStatus(selectedOrder.Id, status);
            }
        }

        // 버튼 활성화 여부 확인 (주문 목록에 항목이 있을 때만 활성화)
        private bool CanAcceptOrder(object parameter)
        {
            return Orders.Count > 0;
        }
    }
}
