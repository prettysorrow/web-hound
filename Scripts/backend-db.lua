local CONTAINER = "web-hound-backend-db"
local SERVICE   = "backend-db"

local function exec(cmd)
    local ok = os.execute(cmd)
    return ok == true or ok == 0
end

local function container_exists()
    return exec('docker ps -a | grep "' .. CONTAINER .. '" > /dev/null 2>&1')
end

local commands = {}

commands["create"] = function()
    if container_exists() then
        print("nothing to do")
        return
    end
    exec('docker compose up -d')
    exec('dotnet ef migrations add FreshBackendDatabase --project Backend')
    exec('dotnet ef database update                     --project Backend')
end

commands["update"] = function()
    if not container_exists() then
        print("database was not created")
        os.exit(1)
    end
    exec('dotnet ef migrations add UpdateBackendDatabase --project Backend')
    exec('dotnet ef database update                      --project Backend')
end

commands["drop"] = function()
    if not container_exists() then
        print("nothing to do")
        return
    end
    exec('docker compose stop "'   .. SERVICE .. '"')
    exec('docker compose down -v "' .. SERVICE .. '"')
    exec('rm -rf Database')
    exec('rm -rf Backend/Migrations')
end

commands["stop"] = function()
    if container_exists() then
        exec('docker compose stop "' .. SERVICE .. '"')
    else
        print("nothing to do")
    end
end

commands["start"] = function()
    if not container_exists() then
        exec('docker compose up -d "' .. SERVICE .. '"')
    end

    exec('docker compose start "' .. SERVICE .. '"')
end

local cmd = arg[1]
if not cmd or not commands[cmd] then
    print("Usage: lua backend.db <command>")
    print("Commands:")
    for k in pairs(commands) do
        print("  " .. k)
    end
    os.exit(1)
end

commands[cmd]()
