#!/bin/bash

# Configuration
PROJECT_PATH="DevHabits.Api/DevHabits.Api.csproj"
MIGRATIONS_PATH="Migrations/Application"

# Menu
while true; do
  echo "=========================="
  echo " EF Core Migration Manager "
  echo "=========================="
  echo "1) Add Migration"
  echo "2) Remove Last Migration"
  echo "3) Update Database"
  echo "4) Exit"
  echo -n "Choose an option [1-4]: "
  read choice

  case $choice in
    1)
      echo -n "Enter migration name: "
      read migrationName
      if [ -z "$migrationName" ]; then
        echo "Migration name cannot be empty."
      else
        dotnet ef migrations add "$migrationName" \
          --project "$PROJECT_PATH" \
          --output-dir "$MIGRATIONS_PATH"
      fi
      ;;
    2)
      dotnet ef migrations remove \
        --project "$PROJECT_PATH"
      ;;
    3)
      dotnet ef database update \
        --project "$PROJECT_PATH"
      ;;
    4)
      echo "Exiting..."
      break
      ;;
    *)
      echo "Invalid choice. Please try again."
      ;;
  esac

  echo ""
done
