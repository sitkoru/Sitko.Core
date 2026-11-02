# Данные

Новых сущностей нет. DeliveryResult<byte[], byte[]> сохраняет исходные Topic,
Partition, Offset, Status и Message. Успех требует Status=Persisted,
непустого Topic, Partition.Value >= 0, Offset.Value >= 0.
