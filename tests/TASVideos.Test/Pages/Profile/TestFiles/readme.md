generated with
```sh
for ext in gif jpg png webp; do
	for size in 124 125 126; do magick -size ${size}x$size canvas: ${size}x.$ext; done
done
cp 125x.jpg truncated.jpg; truncate -s80 truncated.jpg
head -c256 /dev/random >dummy.bin
touch empty.bin
```
