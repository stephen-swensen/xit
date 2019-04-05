FROM fsharp:10.2.1
ENV MONO_THREADS_PER_CPU 50
ENV MONO_GC_PARAMS max-heap-size=768M
COPY .paket/ .paket/
COPY paket.dependencies .
COPY paket.lock .
RUN mono .paket/paket.exe restore
COPY app.fsx app.fsx
ENTRYPOINT ["fsharpi", "app.fsx"]
