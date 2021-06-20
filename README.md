# scrapbook

Problem - you're reading a book that makes reference to artworks, but the book doesn't have the picture printed in it, or the reproduction is poor.

You know it's out there on the web, so you go and find it. Maybe you keep a list of image URLs, web pages - maybe even IIIF manifests - for the images mentioned. The easiest thing for me to do this in is a Google Sheet.

What if you could turn that sheet into a IIIF Manifest for opening in UV or Mirador? Or even annotating?

Make a Google Sheet that looks like this:

![image](https://user-images.githubusercontent.com/1443575/122679064-4925e880-d1e1-11eb-92a0-8e922e5d2a12.png)

(The sheet is here - https://docs.google.com/spreadsheets/d/1i9SwT0-waIuw1vN-6hpGqVdo7pvFh5HoBLNsAXTteM4)

...and then append the sheet URL to this site:

![image](https://user-images.githubusercontent.com/1443575/122679109-7ecad180-d1e1-11eb-8382-df05bea3d330.png)

you'll get redirected to a IIIF Manifest (after some churning):

![image](https://user-images.githubusercontent.com/1443575/122679257-0fa1ad00-d1e2-11eb-8fa1-49067ffc8d51.png)

You can then load this into IIIF viewers:

![image](https://user-images.githubusercontent.com/1443575/122679309-3b249780-d1e2-11eb-9cb7-cafde5a3ab3d.png)

or

![image](https://user-images.githubusercontent.com/1443575/122679359-6c04cc80-d1e2-11eb-9317-96b61971df25.png)

## Warning!

This is very sketchy and only just works at the moment. Your sheet needs to be publicly readable. 
I'd like to get it working with any web page - search the web page for a IIIF Manifest, or a IIIF Image API, and if you can't find them, the largest image. This will be a lot of chipping away at test cases until it works reasonably well.

This is where use of known HTML markup to indicate that a link goes to something IIIF-y would be very useful.

There is no caching at all, every manifest request goes off and visits all your links. If they are large images, that could be very time consuming. It downloads the images to measure their width and height. 

It doesn't do any of this in parallel yet, either. So it will be slow. But this initial version, I just want to see what the spreadsheet feels like and whether this is a good idea.

## How does this work?

The title of the sheet is the title of the manifest.

Blank rows are ignored.

Rows without a link on them become sections in the table of contents. If you don't have rows like this, there won't be a table of contents.

For rows with a link in the first cell, Scrapbook will try to work out what image you want. 
If the link is a Wikimedia page for an image, it will do some special stuff and make an image service (see below).

If the link is a IIIF Image Service, it will just use it.

If the link is a IIIF Manifest, it will use the first image it finds in it.

If the link is to an image directly (e.g., a JPEG), it will wrap that in a IIIF Canvas.

If you want to provide a label for the image or image service, put the text in the second column. 

(not done yet) If the link is to a web page, Scrapbook will search the page for a link to an IIIF resource and use that. 

# Technical info, and special treatment for WikiMedia

Very often, you can find an image in WikiMedia. If the page in the spreadsheet is one of these:

![image](https://user-images.githubusercontent.com/1443575/122679455-d6b60800-d1e2-11eb-878f-4ae1a3814b37.png)

... then it will make use of this proxy service:

![image](https://user-images.githubusercontent.com/1443575/122679533-285e9280-d1e3-11eb-8980-63f360f873ce.png)

redirects to a IIIF Manifest:

![image](https://user-images.githubusercontent.com/1443575/122679550-3b716280-d1e3-11eb-9557-4d7775c4f44b.png)

...which includes a level-0 sizes-only image service, as described at https://tomcrane.github.io/scratch/osd/iiif-sizes.html

See the manifest at https://iiifmediawiki.herokuapp.com/presentation/File:Gustave_Courbet_-_A_Burial_at_Ornans_-_Google_Art_Project_2.jpg

The code for this proxy is at https://github.com/tomcrane/mediawiki-iiifproxy - also a WIP!

Something similar could be done for Flickr, which also has "legacy image pyramids" - i.e., different sizes of the full image - that could be turned into IIIF Image Services by proxying them.

The Wikimedia proxy is in Python and the sheets->manifest code is in .NET. I think I should probably make a Python version of the latter for more general use.



