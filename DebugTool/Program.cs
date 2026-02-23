using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Petek.DebugTool;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Petek Server Debug Tool ===\n");
        
        Console.Write("Server URL (örn: http://localhost:5000): ");
        var serverUrl = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(serverUrl)) serverUrl = "http://localhost:5000";
        
        Console.Write("Kullanıcı adı: ");
        var username = Console.ReadLine()?.Trim();
        
        Console.Write("Şifre (Windows Auth için boş bırakın): ");
        var password = Console.ReadLine()?.Trim();
        
        var client = new HttpClient { BaseAddress = new Uri(serverUrl) };
        
        try
        {
            // 1. Login
            Console.WriteLine("\n1. Login yapılıyor...");
            var loginData = new { username, password };
            var loginJson = JsonSerializer.Serialize(loginData);
            var loginResponse = await client.PostAsync("/api/auth/login", 
                new StringContent(loginJson, null, "application/json"));
            
            if (!loginResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Login başarısız: {loginResponse.StatusCode}");
                Console.WriteLine(await loginResponse.Content.ReadAsStringAsync());
                return;
            }
            
            var loginResult = await loginResponse.Content.ReadAsStringAsync();
            var loginDoc = JsonDocument.Parse(loginResult);
            var token = loginDoc.RootElement.GetProperty("data").GetProperty("token").GetString();
            Console.WriteLine($"✅ Login başarılı! Token alındı.");
            
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // 2. Tüm kullanıcıları listele
            Console.WriteLine("\n2. Tüm kullanıcılar getiriliyor...");
            var usersResponse = await client.GetAsync("/api/users");
            
            if (!usersResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Kullanıcılar getirilemedi: {usersResponse.StatusCode}");
                return;
            }
            
            var usersResult = await usersResponse.Content.ReadAsStringAsync();
            var usersDoc = JsonDocument.Parse(usersResult);
            var users = usersDoc.RootElement.GetProperty("data");
            
            Console.WriteLine($"✅ Toplam {users.GetArrayLength()} kullanıcı bulundu:");
            foreach (var user in users.EnumerateArray())
            {
                var id = user.GetProperty("id").GetString();
                var displayName = user.GetProperty("displayName").GetString();
                var userUsername = user.GetProperty("username").GetString();
                var email = user.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : "N/A";
                var status = user.GetProperty("status").GetString();
                
                Console.WriteLine($"  - {displayName} (@{userUsername}) - {email} [{status}]");
            }
            
            // 3. Kullanıcı arama testi
            Console.WriteLine("\n3. Kullanıcı arama testi...");
            Console.Write("Aramak istediğiniz kelime: ");
            var searchQuery = Console.ReadLine()?.Trim();
            
            if (!string.IsNullOrEmpty(searchQuery))
            {
                var searchResponse = await client.GetAsync($"/api/users/search?q={Uri.EscapeDataString(searchQuery)}");
                
                if (!searchResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Arama başarısız: {searchResponse.StatusCode}");
                    Console.WriteLine(await searchResponse.Content.ReadAsStringAsync());
                    return;
                }
                
                var searchResult = await searchResponse.Content.ReadAsStringAsync();
                var searchDoc = JsonDocument.Parse(searchResult);
                var searchUsers = searchDoc.RootElement.GetProperty("data");
                
                Console.WriteLine($"✅ Arama sonucu: {searchUsers.GetArrayLength()} kullanıcı bulundu:");
                foreach (var user in searchUsers.EnumerateArray())
                {
                    var displayName = user.GetProperty("displayName").GetString();
                    var userUsername = user.GetProperty("username").GetString();
                    var email = user.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : "N/A";
                    
                    Console.WriteLine($"  - {displayName} (@{userUsername}) - {email}");
                }
            }
            
            Console.WriteLine("\n✅ Tüm testler tamamlandı!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ HATA: {ex.Message}");
            Console.WriteLine($"Detay: {ex}");
        }
    }
}
