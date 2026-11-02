# Основания

KafkaFlow 4.2 ProduceAsync возвращает DeliveryResult и не принимает CancellationToken.
Завершающие обработчики могут выбросить исключение после записи. Исключения нельзя
переводить в NotPersisted. Исходный код: Farfetch/kafkaflow commit
2596598798ca8c0b5f0bf23cca263c9ed27c9f92, src/KafkaFlow/Producers/MessageProducer.cs.
Собственная модель и повторы отвергнуты: готовый результат уже содержит нужные сведения,
повтор при неизвестном исходе может создать дубликат.
