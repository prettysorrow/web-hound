local CONTAINER   = "web-hound-backend-db"
local SERVICE     = "backend-db"

local log_tag     = {
    Fatal = "[fatal]",
    Info = "[info]",
    Warning = "[warning]"
}

local log_channel = {
    Fatal = io.stderr,
    Info = io.stdout,
    Warning = io.stderr
}

local function write_log(tag, msg)
    if (msg == nil or msg == "") then
        io.stderr:write("Warning" .. " logger was called with empty message" .. "\n")
        return
    end

    if (tag == nil or tag == "") then
        io.stderr:write("Warning" .. " logger was called with empty tag" .. "\n")
        return
    end

    log_channel[tag]:write(log_tag[tag] .. " " .. msg .. "\n")
end

local function make_failfast(msg)
    if (msg == nil or msg == "") then
        return function()
            os.exit(1)
        end
    end

    return function()
        write_log("Fatal", msg)
        os.exit(1)
    end
end

local function exec(cmd, on_err)
    local rez = os.execute(cmd)
    if (rez ~= 0 and rez ~= true)
    then
        if (on_err ~= nil) then
            on_err()
        else
            write_log("Warning",
                'something went wrong while executing "' .. cmd .. '" but error handler was not specified')
        end
    end
end

local function container_exists()
    local code = os.execute('docker container inspect "' .. CONTAINER .. '" > /dev/null 2>&1')
    return code == true or code == 0
end

local function fresh_migration_name()
    return ('postgre_' .. os.date("%Y_%m_%d_%H_%M_%S"))
end

local function add_migration()
    local migration_name = fresh_migration_name()
    local on_err = make_failfast("can not add migration")
    exec(
        'dotnet ef migrations add'
        .. ' "' .. migration_name .. '"'
        .. ' --project EntityFramework '
        .. ' --startup-project Backend',
        on_err)
    exec('dotnet ef database update '
        .. ' --project EntityFramework '
        .. '--startup-project Backend',
        on_err)
end

local commands = {}

commands["create"] = function()
    if container_exists() then
        write_log("Warning", "container_exists, nothing to do")
        return
    end

    exec('docker compose up -d ' .. SERVICE, make_failfast("can not compose container"))
    add_migration()
end

commands["update"] = function()
    if not container_exists() then
        make_failfast("container does not exist")()
    end

    add_migration()
end

commands["drop"] = function()
    if not container_exists() then
        write_log("Warning", "container does not exist, nothing to do")
        return
    end

    exec('docker compose down -v "' .. SERVICE .. '"', make_failfast("can not drop container"))
    exec('rm -rf Database')
    exec('rm -rf EntityFramework/Migrations')
end

commands["stop"] = function()
    if not container_exists() then
        write_log("Warning", "container does not exist, nothing to do")
        return
    end

    exec('docker compose stop "' .. SERVICE .. '"')
end

commands["start"] = function()
    if not container_exists() then
        write_log("Info", "container does not exist, so i am trying to create it")
        exec('docker compose up -d "' .. SERVICE .. '"', make_failfast("can not create container"))
    end

    exec('docker compose start "' .. SERVICE .. '"', make_failfast("can not start container"))
end

commands["recreate"] = function()
    commands["drop"]()
    commands["start"]()
    commands["update"]()
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
