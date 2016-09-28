# Evaders (Coding challenge idea)
#### Basically:
+ Shoot projectiles
+ Dodge enemy projectiles
+ Collect resources while doing so
+ Use resources to spawn more units

Simple, right?
#### How will this work with bots?
Apart from the math needed to dodge enemy projectiles, lots of decision-making is needed:
+ Should I get hit on purpose to collect a resource?
+ How should I shoot to make the enemy run into it / waste time dodging it?
+ What distance should I keep? (nearer = harder to dodge projectiles)
+ Can I shoot with multiple units to create a "wall" of projectiles (that the enemy cannot dodge)?

#### Other game mechanics:
+ No collision (simplifies pathing)
+ Duplicate units with resources (newly created clone spawning at position of existing unit)
+ Limited map size (circle)
+ (Randomly generated stats, like projectile speed, movement speed, hitbox size, ... for each unit)

#### Who wins?
I have a few ideas regarding this:
+ Queue into competitors / standard bot (ranked, simple ELO system)
+ Submit executable (run in non-network VM) to simulate many matches
+ Normal competition at the end

#### Development
Can be done however you want. You get the packet specification and maybe a library for a few select languages. You also get:
+ A spectator client (to spectate own / other matches)
+ (A local server)

![](http://i.imgur.com/jbp2wHQ.png)




