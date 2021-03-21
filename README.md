# Blind SQL injection script

## Requirements

`dotnet 5.0` 


## Lab setup

Start the DVWA docker container by using:

`docker run --rm -it -p 80:80 vulnerables/web-dvwa`

Then configure the application persistence:

1. Log in into `http://localhost/` using `admin` / `password`.
1. Go to `http://localhost/setup.php` and click `Create / Reset Database`.


## Run the code

`dotnet run`


## Example output

```
Tables found:
	Events
	Statistics
	columns
	engines
	events
	files
	guestbook
	plugins
	profiling
	statistics
	users
	views
```
