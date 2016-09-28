# Evaders (Coding challenge idea)
#### Basically:
+ Shoot projectiles
+ Dodge enemy projectiles
+ Collect resources while doing so
+ Spawn or upgrade units

#### How will this work with bots?
Apart from the math needed to dodge (multiple) enemy projectiles, lots of decision-making is needed:
+ Should I get hit on purpose to collect a resource?
+ How should I shoot to make the enemy run into it / waste time dodging it?
+ Should a unit "buddyblock" for another unit?
+ Can I shoot with multiple units to create a "wall" of projectiles (that the enemy cannot dodge)?

#### Other game mechanics:
+ No collision. Moving all units as one may be strong but you won't be able to collect ressources as fast
+ Create units with collected resources
+ Exchange stats with resources (e.g. Health <=> Projectile speed)
+ Collect healing orbs
+ Going out of the map damages units

#### Who wins?
I have a few ideas regarding this:
+ Queue into competitors / standard bot (ranked, simple ELO system)
+ Submit executable (run in non-network VM) to simulate many matches
+ Normal competition at the end

#### Development
Can be done however you want. You get the packet specification (mostly JSON) and maybe a library for a few select languages. You also get:
+ A spectator client (to spectate own / other matches)
+ (A local server)

![](http://i.imgur.com/jbp2wHQ.png)

## Please join the discussion in the issues!
