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


################################################## execute ##################################################
clear
# docker_install_postgres
# docker_install_mailhog
# solution_initilize
# generate_rsa_keys Auth

