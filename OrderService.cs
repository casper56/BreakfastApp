using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text;

namespace BreakfastApp
{
    public class OrderService
    {
        private string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "orders.json");
        public List<Order> AllOrders { get; private set; } = new List<Order>();

        public OrderService()
        {
            LoadOrders();
        }

        public void LoadOrders()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath, Encoding.UTF8);
                    AllOrders = JsonSerializer.Deserialize<List<Order>>(json) ?? new List<Order>();
                }
                catch { AllOrders = new List<Order>(); }
            }
        }

        public void SaveOrders()
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 讓中文可直接閱讀，不轉義
            };
            string json = JsonSerializer.Serialize(AllOrders, options);
            File.WriteAllText(_filePath, json, Encoding.UTF8); // 明確使用 UTF-8
        }

        public string GenerateOrderId()
        {
            string datePrefix = DateTime.Now.ToString("yyyyMMdd");
            // 找出今天最後一筆訂單
            var lastOrder = AllOrders
                .Where(o => o.OrderId.StartsWith(datePrefix))
                .OrderByDescending(o => o.OrderId)
                .FirstOrDefault();

            int nextNum = 1;
            if (lastOrder != null)
            {
                string suffix = lastOrder.OrderId.Substring(8);
                if (int.TryParse(suffix, out int lastNum))
                {
                    nextNum = lastNum + 1;
                }
            }

            return $"{datePrefix}{nextNum:D4}"; // YYYYMMDD0001
        }

        public void AddOrder(Order order)
        {
            AllOrders.Add(order);
            SaveOrders();
        }

        public List<Order> SearchOrders(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return AllOrders;
            return AllOrders.Where(o => o.OrderId.Contains(query)).ToList();
        }
    }
}