# Merged and live

<!-- Whole change · main fast-forwarded to 9ae344a -->

With Emmz's go-ahead, `main` was fast-forwarded to `9ae344a` and pushed, along with
`change/grpc-content`.

- CI on `9ae344a`: `site` run 42870050149 passed, and `samples` run 42870049988 passed.
- Live site: `/services/grpc/` and `/cheatsheets/grpc/` both return 200. The page has the
  `localhost:5288` inventory client and the new propagation wording, and no "4.999". The cheatsheet
  has the `FromDateTimeOffset`/`AddRange` mapping, and no `Task.FromResult(order)`.

## Next

Offer `/opsx:archive` for grpc-content, and wait for Emmz's yes.
