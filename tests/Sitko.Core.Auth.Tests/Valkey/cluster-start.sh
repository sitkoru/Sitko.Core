#!/bin/sh
set -eu
# Equal internal and published ports preserve Cluster discovery addresses.
for port in 17379 17380 17381 17382 17383 17384; do
    mkdir -p /tmp/cluster-$port
    cat > /tmp/cluster-$port/server.conf <<EOF
bind 0.0.0.0
port $port
protected-mode yes
dir /tmp/cluster-$port
save ""
appendonly no
requirepass cluster-test
masterauth cluster-test
cluster-enabled yes
cluster-config-file nodes.conf
cluster-node-timeout 1000
cluster-announce-ip 127.0.0.1
cluster-announce-port $port
cluster-databases 4
EOF
    valkey-server /tmp/cluster-$port/server.conf &
done
for port in 17379 17380 17381 17382 17383 17384; do
    until valkey-cli -p "$port" -a cluster-test ping 2>/dev/null | grep -q PONG; do sleep 0.1; done
done
valkey-cli -a cluster-test --cluster create \
    127.0.0.1:17379 127.0.0.1:17380 127.0.0.1:17381 \
    127.0.0.1:17382 127.0.0.1:17383 127.0.0.1:17384 \
    --cluster-replicas 1 --cluster-yes
wait
