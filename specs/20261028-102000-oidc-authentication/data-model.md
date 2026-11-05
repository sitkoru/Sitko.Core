# Данные

Существующие поля сохранённого UserToken, ключи Redis DOT_<application>_<environment>_<userId>
(SHA-256) и Data Protection purpose RedisTokenStore сохраняются. Типизированные значения
Duende 4 преобразуются к прежнему строковому представлению при необходимости.
TokenStoreType: None=0, Redis=1, InMemory=2. Идентификатор — UserIdClaimName.
