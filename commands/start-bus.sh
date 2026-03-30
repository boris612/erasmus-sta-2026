#!/usr/bin/env sh

docker run --rm -it --name rabbitmq-erasmus-sta -p 5672:5672 -p 15672:15672 rabbitmq:4-management
