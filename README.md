# Ratty (legacy)

A very simple Remote Administration tool. Originaly a school project.

## Installation

```haskell
$ dotnet restore
$ dotnet run
```

## Usage

```
               __    __          
____________ _/  |__/  |_ ___.__.    __________  _____   ____  __.    
\_  __ \__  \\   __\   __<   |  |    \______   \/  _  \ |    |/ _|    
 |  | \// __ \|  |  |  |  \___  |     |     ___/  /_\  \|      <  
 |__|  (____  /__|  |__|  / ____|     |    |  /    |    \    |  \     
            \/            \/          |____|  \____|__  /____|__ \  
                                                      \/        \/    
Commands:
    list `or` rats          display rats
    rat {no}                select a rat to broadcast to
    rat                     deselect rat (broadcast to / select all)  
    
    dir                     display directory of the selected rat(s)
    cd {dir}                navigate the directory of the selected rat(s)
    cmd                     execute a command via powershell (rat-slide)
    upload {fileName}       upload a localFile to the selected rat(s) (tbi.)
    download {fileName}     download a file from the selected rat(s) (tbi.)
    kill                    send terminate command to the selected rat(s)  
    
    help                    display help
    break `or` exit         exit
```



```
               __    __          
____________ _/  |__/  |_ ___.__.    __________    ________________   
\_  __ \__  \\   __\   __<   |  |    \______   \  /  _  \__    ___/   
 |  | \// __ \|  |  |  |  \___  |    |       _/ /  /_\  \|    |      
 |__|  (____  /__|  |__|  / ____|    |    |   \/    |    \    |    
            \/            \/         |____|_  /\____|__  /____|   
                                             \/         \/    
Commands:
    help
    connect `or` reconnect
    break `or` exit
```


## Development

TODO: Write development instructions here

## Contributors

- [[tzekid]](https://github.com/tzekid)  - creator, ~~maintainer~~
