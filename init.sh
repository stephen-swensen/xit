
xit () {
    if [ "$3" ]; then
        curl -s "localhost:9123/$1?$2" --data-binary @"$3"
    else
        curl -s "localhost:9123/$1?$2"
    fi
}
