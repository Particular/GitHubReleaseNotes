As part of this release we had [3 commits](https://github.com/Particular/NServiceBus/compare/4.3.3...4.3.4) which resulted in [1 issue](https://github.com/Particular/NServiceBus/issues?milestone=51&state=closed) being closed.


## Bugs

### [#1925 When using NServiceBus version 4.3.x and an event is sent through a Distributor it is incorrectly received by multiple workers instead of just one. ](https://github.com/Particular/NServiceBus/issues/1925)

When using the distributor in version `4.3.x`, and the workers are subscribing to an event, having the message mapping for the event in the app.config of the worker causes each worker to handle the event, instead of just one worker.

**If you are affected by this bug:**

 * Update your distributor/worker endpoints to the the 4.3.4 hotfix release.
 * Inspect the current subscription entries for the publisher. Remove any extra subscription entries that are already registered for the workers for the event. 
 * Restart your endpoint. 

##### How to delete your subscription entries when using RavenDB persistence:

###### 1 Open the Raven Management Studio

Browse to your RavenDB url. if RavenDB is installed using the default ports the url is either http://servername:8080 or http://servername:8081, otherwise use the appropriate port number.

###### 2 Find the correct database

Identify the database of the publisher endpoint (the database name matches the endpoint name) and double click to open the database.
![image](https://f.cloud.github.com/assets/854553/2084411/46442fb6-8e21-11e3-9f9b-63e31f53fa50.png)

###### 3 Identify the subscription

You will see the subscriptions list. If there are multiple subscription documents, Identify the subscription document based on the event type.  For example,  `Example.Messages.Events.MyEvent` as specified in the `MessageType` column.
![image](https://f.cloud.github.com/assets/854553/2084414/64545a8a-8e21-11e3-9e40-cbb97e469881.png)

###### 4 Delete the erroneous subscribers
Double click on the document to open the subscription list. Select the worker nodes that have been erroneously subscribed to the event and press delete.
![image](https://f.cloud.github.com/assets/854553/2090460/d49c60a8-8e94-11e3-902f-08c0cd922bb1.png)

Once the entries are removed, click on Save to save the document.
![image](https://f.cloud.github.com/assets/854553/2090480/0e41d9dc-8e95-11e3-8aa6-1c9f8b881d49.png)

##### How to delete your subscription entries when using NHibernate persistence:

###### 1 Find your Database in SQL Management Studio

Using Microsoft SQL Management Studio, connect to the appropriate persistence database specified in the `NServiceBus/Persistence` connection string in the app.config of the endpoint

```xml
<connectionStrings>
 <add name="NServiceBus/Persistence" connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=NSERVICEBUS;Integrated Security=True" />
</connectionStrings>
```

###### 2 Delete the erroneous subscribers
Find out the erroneous subscribers by running
```sql
select * 
    FROM [NServiceBus].[dbo].[Subscription]
    where TypeName='Example.Messages.Events.MyEvent'
```

To clear out the subscriptions for the event `Example.Messages.Events.MyEvent`, delete the subscription entries for the workers. For example:

```sql
delete
    FROM [NServiceBus].[dbo].[Subscription]
    where TypeName='Example.Messages.Events.MyEvent'
    and SubscriberEndpoint in ('Example.NServiceBus.Worker@machine1', 'Example.NServiceBus.Worker@machine2')
```




## Where to get it
You can download this release from:
- Our [website](http://particular.net/downloads)
- Or [nuget](https://www.nuget.org/profiles/nservicebus/)