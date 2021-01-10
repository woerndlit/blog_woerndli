# Introduction

I'm starting the technical content of this blog with a series of articles on how you can build and host your own blog.

For those who just want the technical articles, you cand the list of the current and future articles fo the series here:

1. [Setting up a Static Website with CDN Endpoint onAzure Storage Static Website (the manual way)](./azurestoragestaticwebsite)
2. Using a PowerShell Module to set up a Static Website with CDN Endpoint on Azure Storage Static Website (the PowerShell way)
3. Using an ARM template to set up Static Website with CDN Endpoint (the desired state configuration way)
4. Creating a Blog with Statiq Web and the CleanBlog Theme
5. Automating the deployment of your Blog with GithHub Actions
6. Using Verizon Premium Rule Engine to address the routing needs of the previously created Blog.
7. Adding a comments system to the Blog with Octomments

If your interested in what went into the decision making for choosing a blog engine and hosting technology, please read ahead

## The Past (when I first started a blog)

On my first try to start a blog back in 2008 I went the obvious route and used a Wordpress engine with a MySQL backend hosted at a Web Hosting Provider of my choosing.
Let's take a look what the pros and cons of this decisison were:

### The Pros

* powerfull framework
* big community
* tons of addons

### The Cons

* can get slow if not properly maintained
* can get complicated when using addons
* exploitable if not properly maintained

As a result I was often more involved in maintaining and modifiying the layout and addons of the blog, than in actually writing content.
Of course this is my personal oppinion. Others may have different experiences in maintaining and using Wordpress.

## The Future

To start my new Blog I wanted to use a more lightweight system that is easy deployable.
Additionally, content hosted should be delivered as fast as possible to the end users and costs should be moderate.

When I searched for a system to suit my needs, I came across Static Website Generators which take your content and turn it into a lightweight static website. From the multitude of Static Website Generators, I was intrigued by one that is written in C# .Net.
From there I was able to find multiple possibilties to host static website content and using a content delivery network (CND) to meet my needs.

# The Stack

Here is the stack I ultimately decided to use:

* [Statiq Web as the Static Website Generator](https://statiq.dev/web/)
* [CleanBlog Theme for Statiq Web](https://github.com/statiqdev/CleanBlog)
* [Static website hosting in Azure Storage](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-static-website)
* [CDN Endpoint with a custom domain name](https://docs.microsoft.com/en-us/azure/cdn/cdn-create-new-endpoint)