using Npgsql;
var umsConn = Environment.GetEnvironmentVariable("UMS_CONNECTION") ?? "Host=localhost;Port=5432;Database=ums_db;Username=postgres;Password=postgres";
var hrConn = Environment.GetEnvironmentVariable("HR_CONNECTION") ?? "Host=localhost;Port=5432;Database=hr_microservice_db;Username=hr_user;Password=Hr_Pass_2024!";
Console.WriteLine("Starting HR Migration...");
await using var ums = new NpgsqlConnection(umsConn);
await ums.OpenAsync();
await using var hr = new NpgsqlConnection(hrConn);
await hr.OpenAsync();
var cmd = new NpgsqlCommand("SELECT id, employee_number, full_name, email FROM employees", ums);
await using var reader = await cmd.ExecuteReaderAsync();
int count=0;
while(await reader.ReadAsync()){
 var oldId=reader.GetGuid(0);
 var empNo=reader.IsDBNull(1)?$"ADM-HR-{DateTime.UtcNow:yyyyMM}-{count+1:D5}":reader.GetString(1);
 var fullName=reader.IsDBNull(2)?"Unknown":reader.GetString(2);
 var email=reader.IsDBNull(3)?$"emp{count}@ums.local":reader.GetString(3);
 var newId=Guid.NewGuid();
 var insert=new NpgsqlCommand("INSERT INTO employees (id, external_user_id, employee_number, full_name, email, is_active, created_at) VALUES (@id,@extId,@num,@name,@email,true,NOW()) ON CONFLICT (external_user_id) DO NOTHING",hr);
 insert.Parameters.AddWithValue("id",newId);
 insert.Parameters.AddWithValue("extId",oldId);
 insert.Parameters.AddWithValue("num",empNo);
 insert.Parameters.AddWithValue("name",fullName);
 insert.Parameters.AddWithValue("email",email);
 await insert.ExecuteNonQueryAsync();
 count++; Console.WriteLine($"Migrated {empNo}");
}
Console.WriteLine($"Done {count}");
