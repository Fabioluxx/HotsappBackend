@ECHO OFF
dotnet ef dbcontext scaffold "server=localhost;port=3306;user=root;password=WhatsTrolldev;database=WhatsTroll" MySql.Data.EntityFrameworkCore -o "Model" -f -c "DataContext" --context-dir Context
PAUSE