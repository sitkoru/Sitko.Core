#!/bin/sh
set -eu
# Equal internal and published ports preserve Sentinel discovery addresses.
for port in 16379 16380 16381; do
    mkdir -p /tmp/valkey-$port
    cat > /tmp/valkey-$port/server.conf <<EOF
bind 0.0.0.0
port $port
protected-mode yes
dir /tmp/valkey-$port
save ""
appendonly no
user default on >default-test ~* &* +@all
user app on >valkey-test ~* &* +@all
masteruser app
masterauth valkey-test
replica-announce-ip 127.0.0.1
replica-announce-port $port
EOF
    if [ "$port" != 16379 ]; then
        echo 'replicaof 127.0.0.1 16379' >> /tmp/valkey-$port/server.conf
    fi
    valkey-server /tmp/valkey-$port/server.conf &
done
for port in 26379 26380 26381; do
    cat > /tmp/sentinel-$port.conf <<EOF
bind 0.0.0.0
port $port
user default off
user sentinel-client on >sentinel-test ~* &* +@all
sentinel sentinel-user sentinel-client
sentinel sentinel-pass sentinel-test
sentinel monitor sitko-test 127.0.0.1 16379 2
sentinel auth-user sitko-test app
sentinel auth-pass sitko-test valkey-test
sentinel down-after-milliseconds sitko-test 1000
sentinel failover-timeout sitko-test 10000
sentinel announce-ip 127.0.0.1
sentinel announce-port $port
EOF
    valkey-server /tmp/sentinel-$port.conf --sentinel &
done
wait
