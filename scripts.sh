#!/bin/bash

################################################## variables ##################################################
postgres_image="postgis/postgis:18-master"
postgres_container="postgres"
postgres_user="postgres"
postgres_password="P@ssw0rd"
dbuser_user="dbuser"
dbuser_password="P@ssw0rd"
mailhog_image="mailhog/mailhog:v1.0.1"
mailhog_container="mailhog"

################################################## functions ##################################################

docker_install_postgres(){
  # remove the image
  docker container rm $postgres_container --force
  docker run --name $postgres_container --publish 5432:5432 --detach \
    --env "POSTGRES_USER=$postgres_user" \
    --env "POSTGRES_PASSWORD=$postgres_password" \
    $postgres_image
  sleep 5
  docker exec -it $postgres_container psql -U $postgres_user -c "create user $dbuser_user with superuser password '$dbuser_password';"
}

docker_install_mailhog(){
  # open in http://localhost:8025
  docker container rm $mailhog_container --force
  docker run --name $mailhog_container --detach --publish 587:587 --publish 8025:8025 $mailhog_image
}

solution_initilize(){
  dotnet tool restore
}

generate_rsa_keys() {
  output_dir="${1:-.}/Keys"
  mkdir -p "${1:-.}/Keys"
  openssl genrsa -out "$output_dir/private.key" 2048
  openssl rsa -in "$output_dir/private.key" -pubout -out "$output_dir/public.key"
}

update_dotnet_packages() {
  find . -name "*.csproj" | sort | while read csproj; do
    echo "Processing project: $csproj"
    dotnet list "$csproj" package --outdated | grep -E '>' | awk '{print $2, $4}' |
    while read name version; do
      echo "Updating package: $name to project: $csproj"
      dotnet add "$csproj" package "$name"
    done
  done
  # dotnet list package --outdated
  dotnet tool update --all # Update all tools
}

update_database(){
  project=$1
  startup_project=$2
  rm -rf $project/Migrations # remove migration folder
  # dotnet build
  dotnet ef migrations add Initial --project $project --startup-project $startup_project # create initial migration
  dotnet ef migrations script --project $project --startup-project $startup_project --output $project/Migrations/Script.sql # create script
  dotnet ef database drop --project $project --startup-project $startup_project --force # drop database
  dotnet ef database update --project $project --startup-project $startup_project # update database
}

################################################## execute ##################################################
clear
# docker_install_postgres
# docker_install_mailhog
# solution_initilize
# generate_rsa_keys Auth
# update_dotnet_packages

update_database Auth Auth
