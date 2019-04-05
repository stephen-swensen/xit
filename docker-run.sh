docker kill xit
docker rm xit

docker build -t xit .

docker run --name xit -t -p 9123:9123 xit
docker logs xit -f
