# scrapbook

Make a Google Sheet that looks like this:


...and then append the sheet URL to this site; you'll get a IIIF Manifest.

You can then load this into IIIF viewers:


## Tips

The title of the sheet is the title of the manifest.

Blank rows are ignored.

Rows without a link on them become sections in the table of contents. If you don't have rows like this, there won't be a table of contents.

For rows with a link in the first cell, Scrapbook will try to work out what image you want. 
If the link is a Wikimedia page for an image, it will do some special stuff and make an image service.
(other special providers to follow).

If the link is a IIIF Image Service, it will just use it.

If the link is a IIIF Manifest, it will use the first image it finds in it.

If the link is to an image directly (e.g., a JPEG), it will wrap that in a IIIF Canvas.

If you want to provide a label for the image or image service, put the text in the second column. 

If the link is to a web page, Scrapbook will search the page for a link to an IIIF resource and use that. 

I can keep adding to its understanding of how IIIF appears on web pages.
