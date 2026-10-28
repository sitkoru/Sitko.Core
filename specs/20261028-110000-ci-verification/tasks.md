# Задачи: проверки изменений Sitko.Core

## Фаза 1: Подготовка

- [x] T001 Сверить canonical specs, props и inventory в tests/ с .github/workflows/main.yml; сохранить контракт spec.md.
- [x] T002 Проверить согласованность spec.md, plan.md и tasks.md до реализации.

## Фаза 2: Полнота и наблюдаемость

- [x] T003 [US1] Добавить четыре набора в .github/workflows/main.yml и выполнить их CLI (FR-001, SC-001).
- [x] T004 [US1] Сохранять отдельные TRX/diagnostics и always-artifacts всех тестовых заданий в .github/workflows/main.yml (FR-002, FR-003, SC-002).

## Фаза 3: Подготовка и актуальность PR

- [x] T005 [US2] Зафиксировать образ Main и SDK CodeQL, добавить v13 в .github/workflows/codeql-analysis.yml (FR-004).
- [x] T006 [US2] Добавить PR-only concurrency в main.yml/codeql-analysis.yml и готовность существующих сервисов Main (FR-005, FR-006).
- [x] T007 [US2] Проверить YAML, inventory, сохранение release/npm и ненулевого выхода при ошибке (FR-003, SC-002, SC-003).

## Фаза 4: Документация и сверка

- [x] T008 Обновить docs/ru/testing.md и docs/en/testing.md вместе с CI.
- [x] T009 Сопоставить реализацию с spec.md/plan.md/tasks.md через converge; передать ограничения родителю.

## Зависимости и стратегия

T001 → T002 → T003/T004 → T005/T006 → T007 → T008 → T009.
Все изменения общего workflow выполняются последовательно. Сначала полный список
проектов и отчёты, затем подготовка и проверки. Новые параллельные задания не нужны.

## Фаза 5: Результат converge

Проверены 6 FR, 3 SC, два сценария, решения плана и шесть принципов конституции.
Непрошедшая приёмка: SC-001 (Grpc.Server) и подтверждение SC-002 в GitHub.
Непрошенных изменений и изменений runtime не обнаружено.

- [ ] T010 Повторить tests/Sitko.Core.Grpc.Server.Tests после отдельной поставки исправления R07; получить ненулевой успешный результат (SC-001, partial).
- [ ] T011 Подтвердить обязательный CI на итоговом SHA: сервисы, CodeQL, отчёты/диагностику в артефактах и независимое ревью .github/workflows/main.yml (SC-002, partial).
