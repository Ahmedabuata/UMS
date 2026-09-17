services:
  # ============================================================
  # Databases
  # ============================================================

  # ums-db:
  #   image: postgres:15-alpine
  #   container_name: ums-db
  #   environment:
  #     POSTGRES_DB: ums_db
  #     POSTGRES_USER: ums_user
  #     POSTGRES_PASSWORD: ${POSTGRES_UMS_PASSWORD}
  #   ports:
  #     - "5432:5432"
  #   volumes:
  #     - ums_pgdata:/var/lib/postgresql/data

  hr-db:
    image: postgres:15-alpine
    container_name: hr-db
    environment:
      POSTGRES_DB: hr_microservice_db
      POSTGRES_USER: ums_user
      POSTGRES_PASSWORD: ${POSTGRES_HR_PASSWORD}
    ports:
      - "5433:5432"
    volumes:
      - hr_pgdata:/var/lib/postgresql/data

  identity-db:
    image: postgres:15-alpine
    container_name: identity-db
    environment:
      POSTGRES_DB: identity_db
      POSTGRES_USER: ums_user
      POSTGRES_PASSWORD: ${POSTGRES_IDENTITY_PASSWORD}
    ports:
      - "5434:5432"
    volumes:
      - identity_pgdata:/var/lib/postgresql/data


  # ============================================================
  # Cache and Message Broker
  # ============================================================

  ums-redis:
    image: redis:7-alpine
    container_name: ums-redis
    ports:
      - "6379:6379"

  rabbitmq:
    image: rabbitmq:3-management-alpine
    container_name: rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: ${RABBITMQ_USER}
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD}
    ports:
      - "5672:5672"
      - "15672:15672"
