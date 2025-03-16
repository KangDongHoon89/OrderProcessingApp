using Microsoft.AspNetCore.SignalR.Client;
using OrderProcessingApp.Models;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace OrderProcessingApp
{
    public class OrderSocketClient
    {
        private HubConnection _hubConnection;

        // 주문 수신 이벤트 추가
        public event Action<Order> NewOrderReceived;

        // 주문 상태 변경 이벤트 추가
        public event Action<int, string> OrderStatusUpdated;

        public async Task InitializeAsync()
        {
            try
            {
                // 원격 서버 주소
                var url = "http://localhost:5000/orderHub"; 

                _hubConnection = new HubConnectionBuilder().WithUrl(url).Build();

                // 서버에서 주문을 받으면 이벤트 발생
                _hubConnection.On<int, string, string, int, string, DateTime>("ReceiveNewOrder", (id, orderId, menuItem, quantity, status, createdAt) =>
                {
                    var newOrder = new Order
                    {
                        Id = id,
                        OrderId = orderId,
                        MenuItem = menuItem,
                        Quantity = quantity,
                        Status = status,
                        CreatedAt = createdAt
                    };

                    // 이벤트 호출 (UI에 추가됨)
                    NewOrderReceived?.Invoke(newOrder);

                    MessageBox.Show($"새로운 주문 도착!\n" +
                                    $"주문 ID: {orderId}\n" +
                                    $"메뉴: {menuItem}\n" +
                                    $"수량: {quantity}\n" +
                                    $"상태: {status}\n" +
                                    $"주문 시간: {createdAt}",
                                    "주문 알림",
                                    MessageBoxButton.OK, 
                                    MessageBoxImage.Information);
                });

                // 서버에서 주문 상태 변경 시 실행되는 이벤트 핸들러
                _hubConnection.On<int, string, string>("ReceiveOrderStatusUpdate", (id, orderId, newStatus) =>
                {
                    // UI에 상태 업데이트 적용
                    OrderStatusUpdated?.Invoke(id, newStatus);

                    MessageBox.Show($"주문 {orderId} 상태 변경: {newStatus}",
                                    "주문 업데이트",
                                    MessageBoxButton.OK, 
                                    MessageBoxImage.Information);
                });

                // 자동 재연결 기능 추가
                // 앱 실행 후 네트워크가 끊어졌을 때
                _hubConnection.Closed += async (error) =>
                {
                    // 5초 후 재시도
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    try
                    {
                        await _hubConnection.StartAsync();
                        MessageBox.Show("서버 재연결 성공!", 
                                        "연결 상태", 
                                        MessageBoxButton.OK, 
                                        MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"서버 재연결 실패: {ex.Message}", 
                                        "연결 오류", 
                                        MessageBoxButton.OK, 
                                        MessageBoxImage.Error);
                    }
                };

                // 서버 연결 시도
                await _hubConnection.StartAsync();
            }
            catch (Exception ex)
            {
                // 앱 시작 시 서버가 다운됨
                MessageBox.Show($"서버에 연결할 수 없습니다.\n\n오류: {ex.Message}",
                                "연결 실패",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);

                // 프로그램 안전 종료
                Application.Current.Shutdown();
            }
        }

        // 주문 상태 업데이트 (접수 프로그램 → 서버)
        public async Task SendOrderStatusUpdate(int id, string status)
        {
            try
            {
                if (_hubConnection != null)
                {
                    await _hubConnection.InvokeAsync("UpdateOrderStatus", id, status);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"주문 정보 업데이트 실패.\n\n오류: {ex.Message}",
                                "오류",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }
    }
}
