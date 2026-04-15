using System;
using Microsoft.AspNetCore.Identity;

public class Program {
    public static void Main() {
        var hasher = new PasswordHasher<object>();
        var hash = hasher.HashPassword(new object(), "Admin@123");
        Console.WriteLine(hash);
    }
}
